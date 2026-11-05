# Full-stack Compose startup guide / API 与 Python 总 Compose 启动实操

Prerequisites: Docker Desktop's Linux engine is running and both images have been [built](./docker-image.md). Use the [repository-level Compose file](../../devops/deployment/docker-compose.yaml) to start the API and Python worker together. It includes the existing [API configuration](../../artifact-service-domain/devops/docker/docker-compose.yaml), which remains available for standalone use.
前提：Docker Desktop 的 Linux 引擎已运行，两个镜像均已[构建](./docker-image.md)。使用[仓库级 Compose 文件](../../devops/deployment/docker-compose.yaml)统一启动 API 和 Python worker。它引入已有的 [API 配置](../../artifact-service-domain/devops/docker/docker-compose.yaml)，原文件仍可独立使用。

Run the following steps in the same PowerShell terminal. Continue only after each step succeeds.
在同一个 PowerShell 终端中按顺序执行，每步成功后再继续。

## 1. Set paths and variables / 设置路径与变量

```powershell
$repoPath = "D:\projects\artifact-recognition"
# Office PC: use this instead / 办公室电脑改用：
# $repoPath = "D:\dotNet\personal\projects\artifact-recognition"

$composeFile = Join-Path $repoPath 'devops\deployment\docker-compose.yaml'
$apiPath = Join-Path $repoPath 'artifact-service-domain'
Set-Location $repoPath

$env:CERTIFICATE_NAME = "localhost-artifact.pfx"
$env:API_TLS_HOSTNAME = "localhost-artifact"
# Office PC: use this instead / 办公室电脑改用：
# $env:CERTIFICATE_NAME = "localhost-doraemon.pfx"
# $env:API_TLS_HOSTNAME = "localhost-doraemon"
```

### Check DNS names if the certificate changes / 更换证书时查看 DNS 名称

The current home/office hostnames are listed above. For a different certificate, inspect its SAN before assigning `API_TLS_HOSTNAME`. Inspect the server leaf certificate, not the root CA; enter any PFX password locally.
当前家里和办公室的主机名已列在上面。使用其他证书时，先检查 SAN 再设置 `API_TLS_HOSTNAME`。检查服务端叶子证书，不是根 CA；PFX 密码仅在本机输入。

```powershell
$certPath = Join-Path $apiPath "Configurations\certs\$env:CERTIFICATE_NAME"
$cert = Get-PfxCertificate -FilePath $certPath -ErrorAction Stop

$cert.Extensions |
    Where-Object { $_.Oid.Value -eq '2.5.29.17' } |
    ForEach-Object { $_.Format($true) }

$cert.Dispose()
```

Look for `DNS Name=...` in the SAN output. For example, the office certificate includes:
查看 SAN 输出中的 `DNS Name=...`。例如，办公室证书包含：

```text
DNS Name=localhost
DNS Name=localhost-doraemon
```

家里电脑证书包含：

```text
DNS Name=localhost
DNS Name=localhost-artifact
IP Address=127.0.0.1
IP Address=192.168.31.122
```

Choose a non-`localhost` DNS name covered by your actual certificate, not a filename, IP entry, or URL. If there is no SAN or only `localhost`, stop here; reissue the certificate with a suitable DNS name before using an inter-container alias.
选择实际证书支持的非 `localhost` DNS 名称，不是文件名、IP 条目或 URL。如果没有 SAN 或只有 `localhost`，先停在这里；需重新签发包含合适 DNS 名称的证书，再使用容器间别名。

`CERTIFICATE_NAME` selects the PFX file; `API_TLS_HOSTNAME` sets a Docker network alias covered by the certificate's SAN. The PFX password in the Development configuration must match the certificate. Windows clients must trust its issuer, and the certificate must also cover `localhost` for the access check below.
`CERTIFICATE_NAME` 选择 PFX 文件；`API_TLS_HOSTNAME` 设置证书 SAN 支持的 Docker 网络别名。Development 配置中的 PFX 密码须正确。Windows 客户端须信任签发者，证书还须支持 `localhost`，供下面的访问检查使用。

### Select image tags / 选择镜像标签

Keep both tags from the build terminal. **Only in a new terminal**, select the exact tags of existing images:
保留构建终端中的两个标签。**仅新终端需要**选择已有镜像的准确标签：

```powershell
docker image ls
$env:IMAGE_TAG = Read-Host "Existing artifact-api tag"
$env:WORKER_IMAGE_TAG = Read-Host "Existing artifact-worker tag"
```

Do not leave tags empty or generate new timestamps just for deployment. `${IMAGE_TAG:-dev}` falls back to `dev`, not the newest build. `${API_TLS_HOSTNAME:?...}` rejects an unset or empty hostname.
不要留空标签，也不要仅为了部署生成新时间戳。`${IMAGE_TAG:-dev}` 默认选择 `dev`，不是最新构建；`${API_TLS_HOSTNAME:?...}` 会拒绝未设置或为空的主机名。

### Set the worker's host paths / 设置 worker 的宿主机路径

`API_TRUST_FILE` must point to an existing public certificate in PEM/Base-64 X.509 format, never a private key or PFX. At home, use the root CA public certificate; in the office, use the exported self-signed server public certificate.
`API_TRUST_FILE` 必须指向已有的 PEM/Base-64 X.509 公钥证书，不是私钥或 PFX。家里使用根 CA 公钥证书，办公室使用导出的自签名服务端公钥证书。

```powershell
$env:API_TRUST_FILE = Read-Host "Path to the public trust certificate (PEM or Base-64 CER)"
# Office: replace the Read-Host assignment with this / 办公室可替换上面的交互赋值：
# $env:API_TRUST_FILE = Join-Path $repoPath 'artifact-detection\src\devops\docker\certs\localhost-doraemon-public.pem'

$env:API_TRUST_FILE = (Resolve-Path -LiteralPath $env:API_TRUST_FILE -ErrorAction Stop).Path
$env:WORKER_DATA_PATH = "D:\docker-data\artifact-test"
```

| Variable / 变量    | Container mount / 容器挂载 | Access / 访问方式                                |
| ------------------ | -------------------------- | ------------------------------------------------ |
| `API_TRUST_FILE`   | `/trust/api-public.pem`    | Read-only / 只读                                 |
| `WORKER_DATA_PATH` | `/data`                    | Read/write input and output / 读取输入、写入输出 |

Set all variables before `config` or `up`; they are used when Compose parses the configuration, not when building images.
执行 `config` 或 `up` 前须设置全部变量；Compose 解析配置时就会使用它们，构建镜像时不需要这些挂载路径。

## 2. Check files and validate / 检查文件与验证配置

```powershell
New-Item -ItemType Directory -Path (Join-Path $apiPath 'container-logs') -Force
New-Item -ItemType Directory -Path $env:WORKER_DATA_PATH -Force

Test-Path -LiteralPath (Join-Path $apiPath 'appsettings.Development.json') -PathType Leaf
Test-Path -LiteralPath (Join-Path $apiPath "Configurations\certs\$env:CERTIFICATE_NAME") -PathType Leaf
Test-Path -LiteralPath $env:API_TRUST_FILE -PathType Leaf
Test-Path -LiteralPath $env:WORKER_DATA_PATH -PathType Container

docker compose -f $composeFile config --quiet
$LASTEXITCODE
docker compose -f $composeFile config --images
```

All path checks must return `True`, validation must return `0`, and the images must match `artifact-api:${IMAGE_TAG}` and `artifact-worker:${WORKER_IMAGE_TAG}`. Correct any mismatch before continuing. Directory creation does not clear existing files.
路径检查均须为 `True`，验证退出码须为 `0`，两个镜像须匹配 `artifact-api:${IMAGE_TAG}` 和 `artifact-worker:${WORKER_IMAGE_TAG}`。不一致时先修正再继续。创建目录不会清空已有文件。

## 3. First migration only / 仅首次迁移需要

The combined project's name is `artifact-recognition`. Existing containers created by the standalone API project or `docker run` cannot be automatically adopted when they already occupy `artifact-api` or `artifact-worker`. Check Docker Desktop's project grouping; skip this step if both already belong to `artifact-recognition`.
总项目名为 `artifact-recognition`。旧 API 独立项目或 `docker run` 创建的容器若已占用 `artifact-api`、`artifact-worker`，新项目不能自动接管。查看 Docker Desktop 的项目分组；两者已属于 `artifact-recognition` 时跳过此步骤。

Only stop/remove old containers that actually exist. Back up important container-local files first; bind-mounted Windows data and logs remain. Keep the images for rollback.
仅停止、删除实际存在的旧容器。先备份重要的容器内文件；绑定挂载的 Windows 数据和日志会保留。镜像保留用于回滚。

```powershell
# Skip these two commands if the old worker does not exist / 旧 worker 不存在时跳过：
docker stop -t 10 artifact-worker
docker rm artifact-worker

# Skip these two commands if the old API does not exist / 旧 API 不存在时跳过：
docker stop artifact-api
docker rm artifact-api
```

Do not run the standalone API deployment alongside the combined deployment: they use the same container name and host port.
不要同时运行 API 独立部署和总部署：两者使用相同容器名和宿主机端口。

## 4. Start and check status / 启动并检查状态

```powershell
docker compose -f $composeFile up -d --pull never
docker compose -f $composeFile ps -a
docker compose -f $composeFile logs --tail 20 artifact-api artifact-worker
```

`up -d` applies changes and runs in the background; `--pull never` uses local images only. No manual container deletion or rebuild is needed for alias/port changes.
`up -d` 应用变更并后台运行；`--pull never` 只使用本地镜像。仅修改别名或端口，不需要手动删除容器或重新构建。

Expected: both services `Up`; API environment `Development`, Windows `7092` → container HTTPS `8081`; worker connected and API logs confirm `joined group : recognition-model`. API logs remain in its Windows `container-logs` folder; recognition outputs remain in `WORKER_DATA_PATH`.
预期：两个服务均为 `Up`；API 环境为 `Development`，Windows `7092` → 容器 HTTPS `8081`；worker 连接成功，API 日志确认 `joined group : recognition-model`。API 日志保存在其 Windows `container-logs` 文件夹，识别结果保存在 `WORKER_DATA_PATH`。

## 5. Verify access / 验证访问

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

## 6. Stop, start, or update / 停止、启动或更新

Choose the command you need; do not execute every row in sequence. Keep the variables from step 1 available.
按需选择命令，不要将各行全部依次执行。保持第一步的变量有效。

| Action / 操作                                                                                       | Command / 命令                                      |
| --------------------------------------------------------------------------------------------------- | --------------------------------------------------- |
| Stop both services without deleting / 停止两个服务但不删除                                          | `docker compose -f $composeFile stop`               |
| Start existing containers / 启动已有容器                                                            | `docker compose -f $composeFile start`              |
| Apply changed configuration or image tag / 应用配置或镜像标签变更                                   | `docker compose -f $composeFile up -d --pull never` |
| Follow both services' logs; Ctrl+C only exits log following / 跟踪两个服务的日志，Ctrl+C 只退出跟踪 | `docker compose -f $composeFile logs -f --tail 20`  |

After application code changes, [build the affected image](./docker-image.md) (publish first for the API), keep its new `IMAGE_TAG` or `WORKER_IMAGE_TAG`, then run `up -d --pull never`. In a new terminal, repeat step 1 before using Compose.
应用代码修改后，先[构建受影响的镜像](./docker-image.md)（API 先发布），保留新的 `IMAGE_TAG` 或 `WORKER_IMAGE_TAG`，再执行 `up -d --pull never`。打开新终端后，先重复第一步再使用 Compose。
