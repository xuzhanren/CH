# OpenShift S2I Deployment Guide for CanHappy

This folder contains OpenShift manifests and CI/CD automation for deploying the repository from:
- Git repo: https://github.com/xuzhanren/CH
- DEV source ref: `feature/CH-OS-V1`
- PRD source ref: `develop` (or release tags `v*` if you later switch strategy)

## Final Values Used (Tailored)

- OpenShift API (DEV): `https://api.crc.testing:6443`
- OpenShift API (PRD): `https://api.crc.testing:6443`
- Namespace (DEV): `ch-dev`
- Namespace (PRD): `ch-prd`
- Route host (DEV): `canhappy-dev.apps-crc.testing`
- Route host (PRD): `canhappy-prd.apps-crc.testing`
- Secret name (DEV): `canhappy-secrets-dev`
- Secret name (PRD): `canhappy-secrets-prd`
- DB host (DEV): `postgresql-dev-svc.apps-crc.testing`
- DB host (PRD): `postgresql-dev-svc.apps-crc.testing`

## Folder Structure

- `openshift/base`: shared resources for all environments
- `openshift/overlays/dev`: DEV-specific overrides
- `openshift/overlays/prd`: PRD-specific overrides
- `.github/workflows/openshift-s2i-cicd.yml`: CI/CD pipeline (build, deploy, smoke test)

## What Was Added

1. S2I BuildConfig using Red Hat .NET 10 builder image.
2. ImageStream, Deployment, Service, Route resources.
3. ConfigMap for non-sensitive app settings.
4. Secret injection pattern for DB credentials and external auth/email credentials.
5. DEV/PRD Kustomize overlays.
6. Health endpoint `/healthz` in app for probes and smoke tests.
7. Forwarded headers handling for Route/TLS proxy scenarios.

## Environment Variables and Credentials

### Non-sensitive settings (ConfigMap)
Configured in `openshift/base/configmap.yaml` and overridden by overlays:
- `ASPNETCORE_URLS`
- `ASPNETCORE_ENVIRONMENT`
- `EnableDebug`
- log level settings
- JWT issuer/audience
- SMTP host/port defaults

### Sensitive settings (Secret)
Create the environment-specific secret in each namespace with keys:
- `ConnectionStrings__DefaultConnection`
- `Jwt__Key`
- `Authentication__Google__ClientId`
- `Authentication__Google__ClientSecret`
- `Authentication__Facebook__AppId`
- `Authentication__Facebook__AppSecret`
- `Email__UserName`
- `Email__Password`

A sample key list is in `openshift/base/secrets.example.env`.

## Manual Deployment Steps (CLI)

## 1. Login and select project
```bash
oc login https://api.crc.testing:6443 --token=<DEV_OR_PRD_TOKEN>
oc project ch-dev
```

## 2. Apply manifests
```bash
oc apply -k openshift/overlays/dev
```

For production:
```bash
oc project ch-prd
oc apply -k openshift/overlays/prd
```

## 3. Create/update secret
```bash
oc create secret generic canhappy-secrets-dev \
  --from-literal=ConnectionStrings__DefaultConnection='Host=postgresql-dev-svc.apps-crc.testing;Port=5432;Database=CH;Username=canhappy;Password=<pwd>' \
  --from-literal=Jwt__Key='<strong-random-key>' \
  --from-literal=Email__UserName='<smtp-user>' \
  --from-literal=Email__Password='<smtp-password>' \
  --dry-run=client -o yaml | oc apply -f -
```

Production secret command:
```bash
oc create secret generic canhappy-secrets-prd \
  --from-literal=ConnectionStrings__DefaultConnection='Host=postgresql-dev-svc.apps-crc.testing;Port=5432;Database=CH;Username=canhappy;Password=<pwd>' \
  --from-literal=Jwt__Key='<strong-random-key>' \
  --from-literal=Email__UserName='<smtp-user>' \
  --from-literal=Email__Password='<smtp-password>' \
  --dry-run=client -o yaml | oc apply -f -
```

## 4. Trigger S2I build and deploy
```bash
oc start-build canhappy-dev --follow --wait
oc rollout status deployment/canhappy-dev --timeout=300s
```

Production build/deploy:
```bash
oc start-build canhappy-prd --follow --wait
oc rollout status deployment/canhappy-prd --timeout=600s
```

## 5. Test
```bash
curl -f "https://$(oc get route canhappy-dev -o jsonpath='{.spec.host}')/healthz"
```

Production test:
```bash
curl -f "https://$(oc get route canhappy-prd -o jsonpath='{.spec.host}')/healthz"
```

## CI/CD Flow (GitHub Actions)

Pipeline file: `.github/workflows/openshift-s2i-cicd.yml`

1. Validate: restore/build/test solution.
2. DEV deploy: on push to `feature/CH-OS-V1`.
3. PRD deploy: on push to `main` or `v*` tag.
4. For each environment:
   - Login to OpenShift using GitHub secrets.
   - Apply Kustomize overlay.
   - Create/update environment secret.
   - Trigger `oc start-build` (S2I build from repo).
   - Wait rollout status.
   - Smoke test `/healthz` endpoint.

## GitHub Secrets Required

DEV:
- `OPENSHIFT_API_URL_DEV`
- `OPENSHIFT_TOKEN_DEV`
- `OPENSHIFT_NAMESPACE_DEV`
- `DEV_DB_CONNECTION`
- `DEV_JWT_KEY`
- `DEV_GOOGLE_CLIENT_ID`
- `DEV_GOOGLE_CLIENT_SECRET`
- `DEV_FACEBOOK_APP_ID`
- `DEV_FACEBOOK_APP_SECRET`
- `DEV_EMAIL_USERNAME`
- `DEV_EMAIL_PASSWORD`

PRD:
- `OPENSHIFT_API_URL_PRD`
- `OPENSHIFT_TOKEN_PRD`
- `OPENSHIFT_NAMESPACE_PRD`
- `PRD_DB_CONNECTION`
- `PRD_JWT_KEY`
- `PRD_GOOGLE_CLIENT_ID`
- `PRD_GOOGLE_CLIENT_SECRET`
- `PRD_FACEBOOK_APP_ID`
- `PRD_FACEBOOK_APP_SECRET`
- `PRD_EMAIL_USERNAME`
- `PRD_EMAIL_PASSWORD`

## Release Strategy

1. Merge tested feature changes into `main`.
2. Optional: create `vX.Y.Z` tag for explicit production release.
3. Use GitHub Environment protection rules to require approvals before production job runs.
4. If needed, rollback by redeploying an older image tag or git ref and re-running rollout.

## Notes

- Keep real credentials only in OpenShift Secrets and GitHub Secrets; never commit them.
- If your OpenShift cluster has strict image policy, replace builder image with your allowed internal mirror.
- DEV/PRD route host values in overlays are placeholders; update to your cluster domain.
