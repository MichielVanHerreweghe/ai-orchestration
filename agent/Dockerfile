FROM mcr.microsoft.com/dotnet/sdk:10.0

RUN curl -fsSL https://deb.nodesource.com/setup_24.x | bash - \
    && curl -fsSL https://apt.releases.hashicorp.com/gpg | gpg --dearmor -o /usr/share/keyrings/hashicorp.gpg \
    && echo "deb [signed-by=/usr/share/keyrings/hashicorp.gpg] https://apt.releases.hashicorp.com $(. /etc/os-release && echo $VERSION_CODENAME) main" > /etc/apt/sources.list.d/hashicorp.list \
    && curl -fsSL https://cli.github.com/packages/githubcli-archive-keyring.gpg -o /usr/share/keyrings/githubcli.gpg \
    && echo "deb [signed-by=/usr/share/keyrings/githubcli.gpg] https://cli.github.com/packages stable main" > /etc/apt/sources.list.d/github-cli.list \
    && apt-get update \
    && apt-get install -y --no-install-recommends nodejs terraform openssh-client gh jq \
    && rm -rf /var/lib/apt/lists/*

RUN npm install -g pnpm @anthropic-ai/claude-code

USER app

RUN --mount=type=ssh,uid=1654 \
    mkdir -p ~/.ssh && ssh-keyscan github.com >> ~/.ssh/known_hosts 2>/dev/null \
    && claude plugin marketplace add DietrichGebert/ponytail \
    && claude plugin marketplace add git@github.com:axxitbe/internal-shared-ai.git \
    && claude plugin install ponytail@ponytail \
    && claude plugin install coder@axxit

COPY --chown=app:app CLAUDE.md /home/app/.claude/CLAUDE.md
COPY --chmod=755 entrypoint.sh /usr/local/bin/agent-entrypoint

WORKDIR /workspace
ENTRYPOINT ["agent-entrypoint"]
