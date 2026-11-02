# Compose deployment and worker connection / Compose 部署与 worker 连接速查

First [build the images](./docker-image.md). Run commands in PowerShell, continuing only after each step succeeds. Compose currently manages the API only; the Python command below is a temporary connection test.
先[构建镜像](./docker-image.md)。在 PowerShell 中逐步执行，成功后再继续。目前 Compose 只管理 API；下面的 Python 命令是临时连接测试。

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

`CERTIFICATE_NAME` selects the PFX file; `API_TLS_HOSTNAME` supplies the API's Docker network alias and must match its certificate. The filename does not determine the SAN. The PFX password comes from the mounted Development configuration.
`CERTIFICATE_NAME` 选择 PFX 文件；`API_TLS_HOSTNAME` 提供 API 的 Docker 网络别名，必须匹配证书。文件名不决定 SAN。PFX 密码来自挂载的 Development 配置。

Keep the image tags from the build terminal. **Only in a new terminal**, restore the exact existing tags:
保留构建终端中的镜像标签。**仅新终端需要**恢复已有镜像的准确标签：

```powershell
docker image ls
$env:IMAGE_TAG = Read-Host "Existing artifact-api tag"
$env:WORKER_IMAGE_TAG = Read-Host "Existing artifact-worker tag"
```

Do not leave tags empty or generate new timestamps just for deployment. `${IMAGE_TAG:-dev}` falls back to `dev`, not the newest build. `${API_TLS_HOSTNAME:?...}` rejects an unset or empty hostname.
不要留空标签，也不要仅为了部署生成新时间戳。`${IMAGE_TAG:-dev}` 默认选择 `dev`，不是最新构建；`${API_TLS_HOSTNAME:?...}` 会拒绝未设置或为空的主机名。

## 2. Validate and deploy the API / 验证并部署 API

```powershell
New-Item -ItemType Directory -Path .\container-logs -Force
Test-Path -LiteralPath .\appsettings.Development.json -PathType Leaf
Test-Path -LiteralPath ".\Configurations\certs\$env:CERTIFICATE_NAME" -PathType Leaf

docker compose -f $composeFile config --quiet
$LASTEXITCODE
docker compose -f $composeFile config --images
```

Both path checks must return `True`, validation must return `0`, and the selected image must match `artifact-api:${IMAGE_TAG}`. Then:
两项路径检查须为 `True`，验证退出码须为 `0`，所选镜像须匹配 `artifact-api:${IMAGE_TAG}`。然后执行：

```powershell
docker compose -f $composeFile up -d --pull never
docker compose -f $composeFile ps -a
docker compose -f $composeFile logs --tail 20 artifact-api
```

`up -d` applies changes and runs in the background; `--pull never` uses local images only. No manual container deletion or rebuild is needed for alias/port changes.
`up -d` 应用变更并后台运行；`--pull never` 只使用本地镜像。仅修改别名或端口，不需要手动删除容器或重新构建。

Expected: API `Up`, environment `Development`, Windows `7092` → container HTTPS `8081`. API logs persist in the Windows `container-logs` folder.
预期：API 为 `Up`，环境为 `Development`，Windows `7092` → 容器 HTTPS `8081`。API 日志保存在 Windows 的 `container-logs` 文件夹。

```powershell
curl.exe -i [https://localhost:7092/api/recognition](https://localhost:7092/api/recognition)
```

Expected: `405` with `Allow: POST`, because this request uses GET. Swagger: [https://localhost:7092/swagger](https://localhost:7092/swagger).
预期返回 `405` 且包含 `Allow: POST`，因为这里使用 GET。Swagger：[https://localhost:7092/swagger](https://localhost:7092/swagger)。

For the local development CA only, if Windows Schannel reports unavailable revocation information:
仅针对本地开发 CA，如果 Windows Schannel 报吊销信息不可用：

```powershell
curl.exe -i --ssl-revoke-best-effort [https://localhost:7092/api/recognition](https://localhost:7092/api/recognition)
```

This relaxes missing/offline revocation checks, not chain or hostname validation. It is not a production PKI default; do not replace it with `-k`.
它放宽吊销信息缺失或离线时的检查，不关闭证书链或主机名验证。这不是生产 PKI 默认设置，不要改用 `-k`。

## 3. Prepare Python's public trust file / 准备 Python 公钥信任文件

Windows trust is not automatically inherited by Linux containers. Prepare a PEM certificate file containing no private key:
Linux 容器不会自动继承 Windows 信任。准备不含私钥的 PEM 证书文件：

| Environment / 环境 | Trust material / 信任材料                                                                                                                         |
| ------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------- |
| Home / 家里        | Root CA public certificate, not the leaf PFX. / 根 CA 公钥证书，不是叶子 PFX。                                                                    |
| Office / 办公室    | Self-signed server public certificate exported today: `src\devops\docker\certs\localhost-doraemon-public.pem`. / 今天导出的自签名服务端公钥证书。 |

If needed, export the appropriate public certificate using Windows Certificate Viewer → Details → Copy to File → Base-64 encoded X.509. PEM contains `BEGIN CERTIFICATE`; never export a private key for the worker. The API still needs its PFX.
如需导出，可使用 Windows 证书查看器 → 详细信息 → 复制到文件 → Base-64 编码 X.509。PEM 包含 `BEGIN CERTIFICATE`；不要给 worker 导出私钥。API 仍使用自己的 PFX。

```powershell
Set-Location (Join-Path $repoPath 'artifact-detection')

$apiTrustFile = Read-Host "Path to the trusted public PEM certificate"
$apiTrustFile = (Resolve-Path -LiteralPath $apiTrustFile -ErrorAction Stop).Path
```

## 4. Run the verified SignalR probe / 运行已验证的 SignalR 探测容器

The API's network is normally `docker_default`; if the Compose project name changes, use its actual network. Containers connect through the certificate-matching alias on port `8081`, not Windows port `7092`.
API 网络通常为 `docker_default`；若修改 Compose 项目名，使用实际网络名。容器通过匹配证书的别名和 `8081` 端口连接，不走 Windows 的 `7092`。

```powershell
docker run --rm `
    --name artifact-worker-probe `
    --pull=never `
    --network docker_default `
    --mount "type=bind,source=${apiTrustFile},target=/trust/api-public.pem,readonly" `
    --env "SSL_CERT_FILE=/trust/api-public.pem" `
    --env "REQUESTS_CA_BUNDLE=/trust/api-public.pem" `
    "artifact-worker:${env:WORKER_IMAGE_TAG}" `
    --hub-url "https://${env:API_TLS_HOSTNAME}:8081/hubs/signalr" `
    --result-url "https://${env:API_TLS_HOSTNAME}:8081/api/artifact" `
    --group-name docker-learning-probe `
    --event-name docker-learning-probe
```

`SSL_CERT_FILE` configures Python/OpenSSL trust; `REQUESTS_CA_BUNDLE` supplies trust for Requests-based components. Verification stays enabled. Arguments after the image name go to the Python worker.
`SSL_CERT_FILE` 配置 Python/OpenSSL 信任；`REQUESTS_CA_BUNDLE` 为 Requests 组件提供信任文件。验证保持开启。镜像名后的参数传给 Python worker。

No `-d` means foreground logs. Expected: `Connected to hub`; in another terminal, confirm the API recorded `joined group : docker-learning-probe`:
不加 `-d`，日志在前台显示。预期出现 `Connected to hub`；在另一个终端确认 API 记录了 `joined group : docker-learning-probe`：

```powershell
docker logs --tail 30 artifact-api
```

Stop the probe from another terminal:
从另一个终端停止探测容器：

```powershell
docker stop -t 10 artifact-worker-probe
docker ps -a --filter "name=^/artifact-worker-probe$"
```

`--rm` removes the container after it exits, not the image. Today the worker needed a forced stop (`137`); graceful shutdown remains unresolved. `-t 10` allows a forced kill after the timeout.
`--rm` 在容器退出后删除容器，不删除镜像。今天 worker 被强制停止（`137`），正常关闭问题尚未解决；`-t 10` 超时后允许强制终止。

## 5. Remaining work / 尚待完成

- Map Windows input/output folders to Linux paths. / 将 Windows 输入输出目录映射到 Linux 路径。
- Start the persistent worker and test one real recognition task/result callback. The probe does not join `recognition-model`. / 启动长期运行的 worker，验证一次实际识别及结果回传；探测容器不加入 `recognition-model`。
- Fix and verify graceful shutdown before production use. / 生产使用前修复并验证正常关闭。

## Common API commands / API 常用命令

Use the variables from step 1. `start` reuses the existing container; changed configuration/images require `up -d`.
使用第一步的变量。`start` 启动已有容器；配置或镜像变更使用 `up -d`。

```powershell
docker compose -f $composeFile start artifact-api
docker compose -f $composeFile stop artifact-api
docker compose -f $composeFile logs -f --tail 20 artifact-api
```

For `logs -f`, Ctrl+C only stops following logs. Compose currently does not manage the standalone Python probe.
对于 `logs -f`，Ctrl+C 只退出日志跟踪。目前 Compose 不管理单独启动的 Python 探测容器。
