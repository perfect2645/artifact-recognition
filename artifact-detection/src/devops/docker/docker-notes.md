记录一下python model的，documents\deployment\dockerfile-notes.md

# Dockerfile notes / Dockerfile 知识点

> 参考 .net项目docker文件和python项目docker文件

## Why doesn't .NET create `app`? / 为什么 .NET 不需要创建 `app`？

| Step / 步骤                        | .NET image / .NET 镜像                                                    | Python image / Python 镜像                                          |
| ---------------------------------- | ------------------------------------------------------------------------- | ------------------------------------------------------------------- |
| User creation / 创建用户           | Microsoft created `app` in the base image. 微软已在基础镜像中创建 `app`。 | We create `worker` with `useradd`. 我们用 `useradd` 创建 `worker`。 |
| Select runtime user / 选择运行用户 | `USER app`                                                                | `USER worker`                                                       |

- `useradd`: creates a user inside the image. / 在镜像内创建用户。
- `--create-home`: also creates the user's home directory. / 同时创建用户主目录。
- `--uid 10001`: assigns the numeric user ID. / 指定数字用户 ID。
- `USER`: selects an existing user for subsequent instructions and the default container process; it does not create users. / 选择后续指令及容器默认进程的运行用户，不负责创建用户。

**The difference comes from the base image, not the programming language. `app` is not a built-in Linux user, and Python does not require the name `worker`.**
**区别来自基础镜像，而不是编程语言。`app` 不是 Linux 内置用户，Python 也不要求用户必须叫 `worker`。**
