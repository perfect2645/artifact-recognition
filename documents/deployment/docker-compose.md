# Docker Compose quick guide / Docker Compose 操作速查

Use the existing [Compose configuration](../../artifact-service-domain/devops/docker/docker-compose.yaml). Run all commands in the same PowerShell terminal.
使用现有的 [Compose 配置](../../artifact-service-domain/devops/docker/docker-compose.yaml)，所有命令在同一个 PowerShell 终端执行。

## 1. Prepare / 准备

```powershell
Set-Location 'D:\projects\artifact-recognition\artifact-service-domain'

# Office PC / 办公室电脑：
# Set-Location 'D:\dotNet\personal\projects\artifact-recognition\artifact-service-domain'

$env:CERTIFICATE_NAME = "localhost-artifact.pfx"
# Office certificate / 办公室证书：
# $env:CERTIFICATE_NAME = "localhost-doraemon.pfx"

# Select the image tag to build or deploy / 选择要构建或部署的镜像标签
$env:IMAGE_TAG = "dev-20261030-1.0.0"

docker compose version
New-Item -ItemType Directory -Path .\container-logs -Force

Test-Path -LiteralPath .\appsettings.Development.json -PathType Leaf
Test-Path -LiteralPath ".\Configurations\certs\$env:CERTIFICATE_NAME" -PathType Leaf

```

Both checks must return True. The certificate password in the Development configuration must match the selected certificate. The client must trust the certificate, and it must be valid for localhost.
两项检查必须返回 True。Development 配置中的证书密码必须与所选证书匹配；客户端必须信任证书，且证书须对 localhost 有效。

The image tag is an example: choose a unique tag for each new build. To deploy an existing image, select its exact tag. Compose uses `image: "artifact-api:${IMAGE_TAG:-dev}"`; leaving `IMAGE_TAG` unset or empty selects `dev`, not the latest versioned build.
镜像标签仅为示例：每次新构建选择唯一标签；部署已有镜像时选择其准确标签。Compose 使用 `image: "artifact-api:${IMAGE_TAG:-dev}"`；`IMAGE_TAG` 未设置或为空时会选择 `dev`，而不是最新的版本化镜像。

## 2. Publish and build / 发布与构建

Skip if the required image is already built. Continue only after each command succeeds.
如果已构建所需镜像，可以跳过。每条命令成功后再继续。

```powershell
dotnet publish .\artifact.service.domain.csproj -c Release -o .\publish --self-contained false -p:UseAppHost=false

docker build -f .\devops\docker\Dockerfile.publish -t "artifact-api:${env:IMAGE_TAG}" .

```

## 3. Validate Compose / 验证配置

```powershell
docker compose -f .\devops\docker\docker-compose.yaml config --quiet
$LASTEXITCODE

```

Expected: 0. This validates configuration, not runtime access to files or ports.
预期为 0。这里只验证配置，不验证运行时文件访问或端口可用性。

Check that Compose selects the image tag built above:
确认 Compose 选择了上面构建的镜像标签：

```powershell
docker compose -f .\devops\docker\docker-compose.yaml config --images
```

Expected for the example: `artifact-api:dev-20261030-1.0.0`. Stop if it does not match the intended image.
示例预期输出：`artifact-api:dev-20261030-1.0.0`。如果与目标镜像不一致，先修正再继续。

## 4. Remove a manual container, if present / 如有手动创建的旧容器，先移除

Only for the first migration from docker run. Skip if absent or already managed by this Compose project.
仅用于首次从 docker run 迁移；容器不存在或已由本 Compose 项目管理时跳过。

```powershell
docker ps -a --filter "name=^/artifact-api$"

```

Before removing it, confirm its logs are bind-mounted or back them up. Removing a container deletes its writable layer, but not bind-mounted Windows files.
删除前确认日志已绑定挂载或完成备份。删除容器会删除其可写层，但不会删除绑定挂载的 Windows 文件。

```powershell
docker stop artifact-api
docker rm artifact-api

```

## 5. Start and check / 启动与检查

For an existing Compose deployment, `up -d` recreates the container when the selected image or configuration changes. No manual `stop`/`rm` is needed. Keep the previous image until the new deployment passes verification.
已有 Compose 部署在所选镜像或配置变化时，`up -d` 会重新创建容器，无需手动 `stop`/`rm`。新部署验证通过前保留旧镜像。

```powershell
docker compose -f .\devops\docker\docker-compose.yaml up -d

docker compose -f .\devops\docker\docker-compose.yaml ps -a

docker compose -f .\devops\docker\docker-compose.yaml logs --tail 30 artifact-api

```

Expected: Up, HTTPS listening on 8081, environment Development.
预期：状态为 Up，使用所选版本的镜像，HTTPS 监听 8081，环境为 Development。

## 6. Verify HTTPS and persistent logs / 验证 HTTPS 和持久化日志

```powershell
curl.exe -i --ssl-revoke-best-effort [https://localhost:7092/api/recognition](https://localhost:7092/api/recognition)

Get-ChildItem -LiteralPath .\container-logs -Filter "app-*.log" -File |
    Sort-Object Name -Descending |
    Select-Object -First 1 |
    Get-Content -Tail 10

```

Expected: HTTP 405 with Allow: POST because this request uses GET. Open Swagger at [https://localhost:7092/swagger](https://localhost:7092/swagger) to test POST; an empty artifacts list avoids sending recognition messages.
预期：HTTP 405 且包含 Allow: POST，因为这里发送的是 GET。打开 [https://localhost:7092/swagger](https://localhost:7092/swagger) 测试 POST；使用空 artifacts 列表可避免发送识别消息。

## Common commands / 常用命令

```powershell
# Follow logs; Ctrl+C exits following, not the container / 跟踪日志，Ctrl+C 不会停止容器
docker compose -f .\devops\docker\docker-compose.yaml logs -f --tail 20 artifact-api

# Stop without deleting / 停止但不删除
docker compose -f .\devops\docker\docker-compose.yaml stop

# Start again or apply configuration/image changes / 再次启动或应用配置、镜像变更
docker compose -f .\devops\docker\docker-compose.yaml up -d

# Remove Compose containers and default network; Windows logs remain / 删除容器和默认网络，保留 Windows 日志
docker compose -f .\devops\docker\docker-compose.yaml down

```

After code changes: select a new image tag, then repeat publish → build → up -d. This Compose file does not build the image automatically. In a new terminal, set the project directory, `CERTIFICATE_NAME`, and `IMAGE_TAG` again; use the intended image's exact tag.
修改代码后：先选择新镜像标签，再重复 发布 → 构建 → up -d。此 Compose 文件不会自动构建镜像。打开新终端后，需要重新设置项目目录、`CERTIFICATE_NAME` 和 `IMAGE_TAG`，使用目标镜像的准确标签。

## Certificate chain and revocation / 证书链与吊销

The server (container) must serve the leaf certificate with its private key (`localhost-artifact.pfx`); the root CA public certificate (`Fawei Local Dev Root CA`) belongs in the client's trusted root store, not in the container. The client's chain validates against the trusted root CA.
服务器（container）必须使用叶子证书 + 私钥（`localhost-artifact.pfx`）；根 CA 公钥证书（`Fawei Local Dev Root CA`）属于客户端受信任根存储，不放进容器。客户端链校验到已信任的根 CA 即通过。

The leaf has no CRL or OCSP revocation info, so Schannel's strict revocation check fails with `CRYPT_E_NO_REVOCATION_CHECK`. `--ssl-revoke-best-effort` tolerates the missing revocation info while still validating the chain against the trusted root CA; this is the usual practice for private / internal CAs.
当前叶子证书无 CRL/OCSP 吊销信息，严格吊销检查会报 `CRYPT_E_NO_REVOCATION_CHECK`。`--ssl-revoke-best-effort` 在保留根 CA 链校验的同时容忍缺失吊销信息，这也是私有/内部 CA 客户端的常见做法。

To make plain `curl` (strict revocation) succeed, the leaf must carry CRL/OCSP revocation info: provision an OCSP responder or CRL distribution point for the root CA, then re-issue the leaf with revocation extensions.
若要让普通 `curl`（严格吊销）也通过，叶子证书必须携带 CRL/OCSP 吊销信息：为根 CA 提供 OCSP responder 或 CRL 分发点，并重新签发带吊销扩展的叶子证书。
