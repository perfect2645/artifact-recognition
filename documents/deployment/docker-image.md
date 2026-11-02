# Docker image build guide / Docker 镜像构建速查

Run commands in PowerShell, continuing only after each command succeeds. This guide builds images; use the [Compose guide](./docker-compose.md) to start containers.
在 PowerShell 中逐步执行，每条成功后再继续。本指南负责构建镜像；启动容器请使用 [Compose 指南](./docker-compose.md)。

```text
Web API: source → dotnet publish → docker build → Compose deployment
Web API：源码 → 发布产物 → 构建镜像 → Compose 部署

Python: source + model + requirements → docker build → container
Python：源码 + 模型 + 依赖清单 → 构建镜像 → 容器
```

## 1. Set the repository path / 设置仓库路径

```powershell
$repoPath = "D:\projects\artifact-recognition"
# Office PC: use this instead / 办公室电脑改用：
# $repoPath = "D:\dotNet\personal\projects\artifact-recognition"
```

## 2. Publish and build the Web API / 发布并构建 Web API

```powershell
Set-Location (Join-Path $repoPath 'artifact-service-domain')

dotnet publish .\artifact.service.domain.csproj -c Release -o .\publish --self-contained false -p:UseAppHost=false
```

- `--self-contained false`: the ASP.NET Core base image supplies the runtime. / 运行时由 ASP.NET Core 基础镜像提供。
- `-p:UseAppHost=false`: omit the native launcher; start with `dotnet ...dll`. / 不生成原生启动程序，通过 `dotnet ...dll` 启动。

After publishing succeeds:
发布成功后：

```powershell
$env:IMAGE_TAG = "dev-$(Get-Date -Format 'yyyyMMdd-HHmmss')"

docker build -f .\devops\docker\Dockerfile.publish -t "artifact-api:${env:IMAGE_TAG}" .
```

The [Dockerfile](../../artifact-service-domain/devops/docker/Dockerfile.publish) copies existing published files. After changing C# code, publish again before building; building alone does not compile it.
[Dockerfile](../../artifact-service-domain/devops/docker/Dockerfile.publish) 复制已有发布产物。修改 C# 代码后先重新发布；仅构建镜像不会编译源码。

## 3. Build the Python worker / 构建 Python worker

```powershell
Set-Location (Join-Path $repoPath 'artifact-detection')

$env:WORKER_IMAGE_TAG = "dev-$(Get-Date -Format 'yyyyMMdd-HHmmss')"

docker build -f .\src\devops\docker\Dockerfile -t "artifact-worker:${env:WORKER_IMAGE_TAG}" .
```

No separate publish step is needed. The [Dockerfile](../../artifact-detection/src/devops/docker/Dockerfile) installs CPU PyTorch and other dependencies, copies code and model weights, and selects `worker` (UID `10001`) as the runtime user.
无需单独发布。[Dockerfile](../../artifact-detection/src/devops/docker/Dockerfile) 安装 CPU PyTorch 等依赖，复制代码与模型权重，并选择 `worker`（UID `10001`）运行应用。

The model must exist at `src\model\best_model.pth`. Dockerfile instructions such as `RUN` belong in the file, not in PowerShell.
模型须位于 `src\model\best_model.pth`。`RUN` 等 Dockerfile 指令写在文件里，不要在 PowerShell 执行。

## 4. Check the images / 查看镜像

```powershell
docker image ls

"API image: artifact-api:${env:IMAGE_TAG}"
"Worker image: artifact-worker:${env:WORKER_IMAGE_TAG}"
```

| Part / 参数            | Meaning / 含义                                                                                                                                     |
| ---------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------- |
| `-f` / `--file`        | Select the Dockerfile. / 指定 Dockerfile。                                                                                                         |
| `-t` / `--tag`         | Assign the image name and tag. / 设置镜像名称与标签。                                                                                              |
| Final `.` / 最后的 `.` | Use the current project directory as the build context; `COPY` sources are relative to it. / 当前项目目录作为构建上下文，`COPY` 源路径基于它解析。 |

**Generate each tag once before building; deploy that same tag.** `IMAGE_TAG` belongs to the API; `WORKER_IMAGE_TAG` belongs to Python. A timestamp identifies a build by convention, not its exact source revision.
**每次构建前只生成一次标签，部署时复用同一个标签。** `IMAGE_TAG` 属于 API，`WORKER_IMAGE_TAG` 属于 Python。时间戳按约定区分构建，不代表精确源码版本。

`CACHED` means unchanged build steps were reused. Building does not update running containers. Keep old images until the new deployment passes verification.
`CACHED` 表示复用未变化的构建步骤。构建不会更新运行中的容器；新部署验证通过前保留旧镜像。

Continue in the same terminal with the [Compose guide](./docker-compose.md). In a new terminal, restore the repository path and exact built tags instead of generating new timestamps.
继续在同一终端使用 [Compose 指南](./docker-compose.md)。新终端需恢复仓库路径和已构建的准确标签，不要重新生成时间戳。
