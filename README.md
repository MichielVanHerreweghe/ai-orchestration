# AI orchestration

A GitHub webhook starts a Claude Code agent on an issue when a trusted author comments `/feature-plan` or `/feature-implement`.

Every pull request labelled `preview` gets a preview: the app at its head commit, in a namespace of its own, with its URL commented on the pull request. The agent labels the pull requests it opens. Closing the pull request removes the preview.

- `orchestrator/` receives the webhook and is the Kubernetes operator: each command becomes an `AgentRun`, which the operator runs as a Job of the `agent/` image. Runs on the same issue go one at a time.
- `deploy/` holds the manifests. `agentrun-crd.yaml` is generated from `V1AgentRun`:
  `cd orchestrator && dotnet tool restore && dotnet tool run kubeops -- generate operator --out <tmp> orchestrator src/Orchestrator.Infrastructure/Orchestrator.Infrastructure.csproj`, then copy the CRD from `<tmp>`.
- `demo/` is a small app (API, SQL Server, Angular frontend) that previews this repository's own pull requests.

## Previews

- `.github/workflows/preview.yml` builds the pull request's images (amd64 and arm64) and pushes them to GHCR, tagged with its head commit. Pull requests from forks get none.
- `deploy/argocd/previews.yaml` is an Argo CD ApplicationSet: one Application per pull request labelled `preview`, deploying `preview/` (a kustomization, database included) at its head commit with those images. Only people with write access can label, so a fork's pull request can't deploy itself.
- Argo CD Notifications comments `http://web.<namespace>.svc.cluster.local` on the pull request once a commit is synced and healthy. That URL resolves on a Mac running OrbStack; another cluster needs an Ingress and a different URL in `deploy/argocd/notifications.yaml`.

Another repository gets previews with a `preview/` kustomization, its own copy of the workflow (listing its images) and of the ApplicationSet.

## Deploy

```sh
docker build --ssh default -t agent:local agent/
dotnet publish orchestrator/src/Orchestrator.Api -t:PublishContainer -p:ContainerRepository=orchestrator -p:ContainerImageTag=local

kubectl apply -f deploy/
kubectl -n orchestrator create secret generic agent --from-env-file=agent/.env
kubectl -n orchestrator create secret generic orchestrator --from-literal=webhook-secret=<GitHub webhook secret>

kubectl apply --server-side --force-conflicts -k deploy/argocd
kubectl -n argocd create secret generic github-token --from-literal=token=<GitHub token>
kubectl -n argocd patch secret argocd-notifications-secret --type merge -p '{"stringData":{"github-token":"<GitHub token>"}}'
kubectl apply -f deploy/argocd/previews.yaml
```

On a cluster that can't see local images, push both to a registry and point the Deployment's image and `Agent__Image` at it.

Point the GitHub webhook (content type `application/json`, event "Issue comments") at `http://orchestrator.orchestrator/webhooks/github` through an Ingress or a tunnel. Locally:

```sh
kubectl -n orchestrator port-forward svc/orchestrator 8080:80
gh webhook forward --repo=<owner>/<repo> --events=issue_comment --url=http://localhost:8080/webhooks/github \
  --secret="$(dotnet user-secrets list --project orchestrator/src/Orchestrator.Api | sed -n 's/^GitHub:WebhookSecret = //p')"
```

## Watch and cancel

- `k9s` → `:agentruns` shows repository, issue, command, phase, cost and turns; the run's Job and pod hold the logs for a day.
- Deleting an `AgentRun` cancels it: its Job and pod go with it.
- `:applications` in the `argocd` namespace shows each preview's sync and health status.
