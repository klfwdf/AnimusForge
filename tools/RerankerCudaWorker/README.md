# 可选 CUDA 重排序组件

MCM → AnimusForge → 知识检索（返回）→ **重排序推理设备**：

- **CPU（默认）**：无需此组件，保留游戏内现有 CPU 运行库。
- **GPU（CUDA / NVIDIA）**：安装此组件并选择 GPU 后，完整退出并重启游戏。使用 NVIDIA 的默认 CUDA 设备 0；不支持 AMD/Intel GPU。

组件目录为 `Modules/AnimusForge/OptionalRuntimes/RerankerCuda/`，与 `bin`、`ONNX` 平级。保留包内完整文件，不把这些 DLL 复制到游戏 `bin` 中。正常模组构建/覆盖流程不变，此目录作为可选组件独立安装。模型仍从同一个 `Modules/AnimusForge/ONNX/reranker` 读取，不需要再下载模型。

需要兼容 CUDA 12.8 / cuDNN 9 的 NVIDIA 驱动，无需 Python、系统 CUDA Toolkit 或修改 PATH。组件随带 ORT 1.22.0、CUDA 12.8、cuDNN 9.8 所需运行库。下载和磁盘占用较大；离线 RTX 4060 Laptop 实测全设备显存增量峰值约 2.56 GiB，不是显存上限或 5060 保证。游戏与 GPU 重排序会争用显存。

向量模型、分词、缓存仍在 CPU；只将重排序推理送到常驻的独立进程。同一模型、512-token上限、候选、padding、Sigmoid及缓存规则保持，FP32且关闭TF32。请求串行，缓存命中不进入GPU。GPU故障、工作进程退出/超时，当前请求回退CPU，本次游戏不自动重启GPU进程。启动超时120秒，单次请求超时30秒；超过1024行的大批量直接走CPU，候选不截断。进程随游戏退出释放显存。

查看用户数据 `Logs/Mod_Logic.txt` 的 `OnnxReranker`：`requested=CUDA active=CUDA worker_ready` 表示CUDA会话建立；`active=CPU fallback=...` 表示实际回退。不会悄悄把下拉选项改回CPU；重启可重新尝试。尚需真实游戏长时运行、帧时间、显存压力及目标显卡验收。

## 开发者本地构建

在仓库内运行（输出仅在 `artifacts`，不安装或部署）：

```powershell
python tools/RerankerCpuCudaBenchmark/prepare_dependencies.py --output artifacts/cuda-deps
python tools/RerankerCudaWorker/build_optional_pack.py --deps artifacts/cuda-deps --output artifacts/cuda-optional-build --zip
```

已有独立基准的 `deps` 可直接复用。打包脚本校验原依赖包SHA256，保留许可证/NOTICE及文件清单。生成独立组件包不改变正式模组的一键构建脚本。玩家安装时将包中的 `AnimusForge` 合并到游戏 `Modules/AnimusForge`；卸载可选组件前先退出游戏，再删除仅 `OptionalRuntimes/RerankerCuda` 目录，CPU运行库不受影响。
