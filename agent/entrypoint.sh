#!/bin/bash
# Usage: docker run -e REPO=owner/repo -e ISSUE=7 agent "<prompt>"
set -euo pipefail
set -x

gh auth setup-git
git config --global user.name "$(gh api user -q '.name // .login')"
git config --global user.email "$(gh api user -q '"\(.id)+\(.login)@users.noreply.github.com"')"
gh repo clone "$REPO" .bare -- --bare --quiet
echo "gitdir: ./.bare" > .git
git config remote.origin.fetch "+refs/heads/*:refs/remotes/origin/*"
git fetch --quiet origin

BASE=$(gh repo view "$REPO" --json defaultBranchRef -q .defaultBranchRef.name)
git worktree add --quiet "$BASE" "$BASE"

# An earlier phase's feature branch for this issue: the branch that adds a plan pointing at the issue.
BRANCH=$(for b in $(git for-each-ref --format='%(refname:short)' refs/heads | grep -vx "$BASE"); do
  for f in $(git diff --name-only "$BASE...$b" -- '*implementation-plan.md'); do
    git show "$b:$f" | grep -qE "^issue:.*/issues/$ISSUE\s*$" && echo "$b"
  done
done | head -1 || true)

if [ -n "$BRANCH" ]; then
  git worktree add --quiet "$BRANCH" "$BRANCH"
  cd "$BRANCH"
  BEFORE=$(git rev-parse HEAD)
else
  cd "$BASE"
fi

set +x
echo "[agent] prompt: $1"

# Stream every message, tool call and tool result, rendered as readable log lines.
status=0
STREAM=$(mktemp)
claude -p "$1" --dangerously-skip-permissions --output-format stream-json --verbose \
  | tee "$STREAM" \
  | jq -r --unbuffered '
      def text: if type == "string" then . else map(select(.type == "text") | .text) | join("\n") end;
      (if .parent_tool_use_id then "  [subagent] " else "" end) as $p
      | if .type == "system" and .subtype == "init" then "[init] model=\(.model) cwd=\(.cwd)"
        elif .type == "assistant" then .message.content[]
          | if .type == "text" then "\($p)[claude] \(.text)"
            elif .type == "thinking" then "\($p)[thinking] \(.thinking)"
            elif .type == "tool_use" then "\($p)[tool] \(.name) \(.input | tojson)"
            else empty end
        elif .type == "user" then .message.content[]? | select(.type == "tool_result")
          | "\($p)[result\(if .is_error then " error" else "" end)] \(.content | text)"
        elif .type == "result" then "[done] \(.subtype) turns=\(.num_turns) cost=$\(.total_cost_usd)\n\(.result // "")"
        else "[\(.type)] \(tojson)" end' \
  || status=$?

# In Kubernetes, the operator reads the run's cost and turns from the container's termination message.
if [ -w /dev/termination-log ]; then
  jq -c 'select(.type == "result") | {costUsd: .total_cost_usd, turns: .num_turns}' "$STREAM" > /dev/termination-log || true
fi

# Push every branch the agent worked on, never the default branch, so the next phase's container finds it.
# Runs even after a failed run: implementation commits per milestone, and partial work is worth keeping.
for b in $(git worktree list --porcelain | sed -n 's|^branch refs/heads/||p' | grep -vx "$BASE" || true); do
  echo "[agent] pushing $b"
  git push --quiet -u origin "$b"

  # The preview label is what makes Argo CD deploy a pull request's preview.
  if gh pr view "$b" --repo "$REPO" --json number > /dev/null 2>&1; then
    gh label create preview --repo "$REPO" --color 1d76db --description "Deployed as a preview" > /dev/null 2>&1 || true
    gh pr edit "$b" --repo "$REPO" --add-label preview > /dev/null && echo "[agent] labelled the pull request of $b for a preview"
  fi
done

# After planning, keep one plan comment on the issue (committed or not, without the YAML front-matter).
# First plan: post it. Revision: edit it in place, and reply with Claude's answer to the feedback and the diff.
if [[ "$1" == /feature-plan* ]]; then
  PLAN=$(for wt in $(git worktree list --porcelain | sed -n 's/^worktree //p'); do
    { git -C "$wt" diff --name-only --diff-filter=AM "origin/$BASE"; git -C "$wt" ls-files --others --exclude-standard; } 2>/dev/null \
      | grep 'implementation-plan\.md$' | sed "s|^|$wt/|" || true
  done | head -1)

  if [ -n "$PLAN" ]; then
    MARKER="<!-- agent:plan -->"
    BODY=$(printf '%s\n' "$MARKER"; awk 'NR == 1 && $0 == "---" { fm = 1; next } fm && $0 == "---" { fm = 0; next } !fm' "$PLAN")
    read -r COMMENT_ID COMMENT_URL < <(gh api "repos/$REPO/issues/$ISSUE/comments" --paginate \
      --jq ".[] | select(.body | startswith(\"$MARKER\")) | \"\(.id) \(.html_url)\"" | tail -1) || true

    if [ -n "${COMMENT_ID:-}" ]; then
      echo "[agent] updating plan comment $COMMENT_URL"
      gh api -X PATCH "repos/$REPO/issues/comments/$COMMENT_ID" -f body="$BODY" > /dev/null
      {
        jq -r 'select(.type == "result") | .result // empty' "$STREAM"
        printf '\n[Updated plan](%s)' "$COMMENT_URL"
        [ -n "${BEFORE:-}" ] && printf ' · [Changes](https://github.com/%s/compare/%s...%s)' "$REPO" "$BEFORE" "$(git rev-parse HEAD)"
        echo
      } | gh issue comment "$ISSUE" --repo "$REPO" --body-file -
    else
      echo "[agent] commenting $PLAN on #$ISSUE"
      printf '%s\n' "$BODY" | gh issue comment "$ISSUE" --repo "$REPO" --body-file -
    fi
  else
    echo "[agent] no new implementation-plan.md; nothing to comment"
  fi
fi

exit "$status"
