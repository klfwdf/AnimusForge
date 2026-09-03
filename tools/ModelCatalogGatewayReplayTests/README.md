# ModelCatalog gateway replay

该 runner 只验证 `LegacyModelCatalogGateway` 的离线 HTTP 边界，不加载 Bannerlord、
不读取存档，也不改变模型解析或 UI 状态机。稳定错误码为：

- `model_catalog.url_missing`
- `model_catalog.api_key_missing`
- `model_catalog.cancelled`
- `model_catalog.http_failure`
- `model_catalog.transport_failed`

`ErrorArguments` 只保留有界的 `status`、`reason` 和 `exceptionType`，不会包含 API Key、
完整响应或带查询参数的敏感 URL；`ErrorMessage` 继续保留为兼容字段。

```powershell
dotnet run --project .\tools\ModelCatalogGatewayReplayTests\ModelCatalogGatewayReplayTests.csproj
```
