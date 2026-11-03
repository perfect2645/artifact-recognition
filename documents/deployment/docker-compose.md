# Docker Compose startup guide / Docker Compose 启动实操

Prerequisites: Docker Desktop's Linux engine is running and the API image has been [built](./docker-image.md). The current [Compose configuration](../../artifact-service-domain/devops/docker/docker-compose.yaml) starts the Web API only; it does not start the Python worker.
前提：Docker Desktop 的 Linux 引擎已运行，API 镜像已[构建](./docker-image.md)。当前 [Compose 配置](../../artifact-service-domain/devops/docker/docker-compose.yaml)只启动 Web API，不会启动 Python worker。

Run the following steps in the same PowerShell terminal. Continue only after each step succeeds.
在同一个 PowerShell 终端中按顺序执行，每步成功后再继续。

## 1. Set paths and variables / 设置路径与变量

```powershell
$repoPath = "D:\projects\artifact-recognition"
# Office PC: use this instead / 办公室电脑改用：
# $repoPath = "D:\dotNet\personal\projects\artifact-recognition"

$composeFile = Join-Path $repoPath 'artifact-service-domain\devops\docker\docker-compose.yaml'
Set-Location (Join-Path $repoPath 'artifact-service-domain')

$env:CERTIFICATE_NAME = "localhost-artifact.pfx"
$env:API_TLS_HOSTNAME = Read-Host "DNS hostname covered by the certificate SAN (not localhost)"
# Office PC: replace the two assignments above / 办公室替换上面两项：
# $env:CERTIFICATE_NAME = "localhost-doraemon.pfx"
# $env:API_TLS_HOSTNAME = "localhost-doraemon"
```

`CERTIFICATE_NAME` selects the PFX file; `API_TLS_HOSTNAME` sets a Docker network alias covered by the certificate's SAN. The PFX password in the Development configuration must match the certificate. Windows clients must trust its issuer, and the certificate must also cover `localhost` for the access check below.
`CERTIFICATE_NAME` 选择 PFX 文件；`API_TLS_HOSTNAME` 设置证书 SAN 支持的 Docker 网络别名。Development 配置中的 PFX 密码须正确。Windows 客户端须信任签发者，证书还须支持 `localhost`，供下面的访问检查使用。

Keep `IMAGE_TAG` from the build terminal. **Only in a new terminal**, select the tag of an existing API image:
保留构建终端中的 `IMAGE_TAG`。**仅新终端需要**选择一个已有 API 镜像的标签：

```powershell
docker image ls artifact-api
$env:IMAGE_TAG = Read-Host "Existing artifact-api tag"
```

Do not leave tags empty or generate new timestamps just for deployment. `${IMAGE_TAG:-dev}` falls back to `dev`, not the newest build. `${API_TLS_HOSTNAME:?...}` rejects an unset or empty hostname.
不要留空标签，也不要仅为了部署生成新时间戳。`${IMAGE_TAG:-dev}` 默认选择 `dev`，不是最新构建；`${API_TLS_HOSTNAME:?...}` 会拒绝未设置或为空的主机名。

## 2. Check files and validate / 检查文件与验证配置

```powershell
New-Item -ItemType Directory -Path .\container-logs -Force
Test-Path -LiteralPath .\appsettings.Development.json -PathType Leaf
Test-Path -LiteralPath ".\Configurations\certs\$env:CERTIFICATE_NAME" -PathType Leaf

docker compose -f $composeFile config --quiet
$LASTEXITCODE
docker compose -f $composeFile config --images
```

Both path checks must return `True`, validation must return `0`, and the selected image must match `artifact-api:${IMAGE_TAG}`. Correct any mismatch before continuing.
两项路径检查须为 `True`，验证退出码须为 `0`，所选镜像须匹配 `artifact-api:${IMAGE_TAG}`。不一致时先修正再继续。

## 3. Start and check status / 启动并检查状态

```powershell
docker compose -f $composeFile up -d --pull never
docker compose -f $composeFile ps -a
docker compose -f $composeFile logs --tail 20 artifact-api
```

`up -d` applies changes and runs in the background; `--pull never` uses local images only. No manual container deletion or rebuild is needed for alias/port changes.
`up -d` 应用变更并后台运行；`--pull never` 只使用本地镜像。仅修改别名或端口，不需要手动删除容器或重新构建。

Expected: API `Up`, environment `Development`, Windows `7092` → container HTTPS `8081`. API logs persist in the Windows `container-logs` folder.
预期：API 为 `Up`，环境为 `Development`，Windows `7092` → 容器 HTTPS `8081`。API 日志保存在 Windows 的 `container-logs` 文件夹。

## 4. Verify access / 验证访问

```powershell
curl.exe -i [https://localhost:7092/api/recognition](https://localhost:7092/api/recognition)
```

Expected: `405` with `Allow: POST`, because this request uses GET. Swagger: [[https://localhost:7092/swagger](https://localhost:7092/swagger)](https://localhost:7092/swagger).
预期返回 `405` 且包含 `Allow: POST`，因为这里使用 GET。Swagger：[[https://localhost:7092/swagger](https://localhost:7092/swagger)](https://localhost:7092/swagger)。

For the local development CA only, if Windows Schannel reports unavailable revocation information:
仅针对本地开发 CA，如果 Windows Schannel 报吊销信息不可用：

```powershell
curl.exe -i --ssl-revoke-best-effort [https://localhost:7092/api/recognition](https://localhost:7092/api/recognition)
```

This relaxes missing/offline revocation checks, not chain or hostname validation. It is not a production PKI default; do not replace it with `-k`.
它放宽吊销信息缺失或离线时的检查，不关闭证书链或主机名验证。这不是生产 PKI 默认设置，不要改用 `-k`。

## 5. Stop, start, or update / 停止、启动或更新

Choose the command you need; do not execute every row in sequence. Keep the variables from step 1 available.
按需选择命令，不要将各行全部依次执行。保持第一步的变量有效。

| Action / 操作                                                              | Command / 命令                                                  |
| -------------------------------------------------------------------------- | --------------------------------------------------------------- |
| Stop without deleting / 停止但不删除                                       | `docker compose -f $composeFile stop artifact-api`              |
| Start the existing container / 启动已有容器                                | `docker compose -f $composeFile start artifact-api`             |
| Apply changed configuration or image tag / 应用配置或镜像标签变更          | `docker compose -f $composeFile up -d --pull never`             |
| Follow logs; Ctrl+C only exits log following / 跟踪日志，Ctrl+C 只退出跟踪 | `docker compose -f $composeFile logs -f --tail 20 artifact-api` |

After application code changes, [publish and build a new image](./docker-image.md), keep its `IMAGE_TAG`, then run `up -d --pull never`. In a new terminal, repeat step 1 before using Compose.
应用代码修改后，先[发布并构建新镜像](./docker-image.md)，保留它的 `IMAGE_TAG`，再执行 `up -d --pull never`。打开新终端后，先重复第一步再使用 Compose。
