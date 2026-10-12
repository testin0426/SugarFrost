# 体积云生成工具（Volumetric Cloud）

基于 Compute Shader 生成 3D 噪声贴图，再用 Ray Marching 在盒体网格内采样累积密度与颜色，实现可以在场景里滚动的体积云。仅适用于 URP。

## 文件一览

| 文件 | 说明 |
| --- | --- |
| `CloudNoiseGen.compute` | 生成 3D 噪声贴图：R 通道为低频 billowy 云形，G 通道为高频细节（渲染时用于侵蚀边缘），B 通道保留，A 通道为 1 |
| `CloudNoiseGenerator.cs` | 驱动组件：`Start` 时生成一次噪声，勾选 `animateNoise` 后每帧重新生成 |
| `Cloud_URP.shader` | 体积云渲染：射线步进采样 3D 噪声，累积透过率与颜色，支持风场滚动与 URP 雾效 |
| `New Render Texture.renderTexture` | 示例 3D 噪声贴图（64³，Enable Random Write） |
| `*.mat` | 材质为本地二进制资产，不随仓库同步，需要时按下方步骤自行创建 |

## 使用步骤

1. **创建 3D RenderTexture**：Width/Height/Depth 均为 64（或 128），Dimension 选 `3D`，勾选 `Enable Random Write`，Format 推荐 `ARGB Half`。
2. **创建材质**：Shader 选择 `Custom/URP/Cloud`，把 3D RenderTexture 赋给 `_VoluimeTex`（属性名保留了原始拼写）。
3. **搭建云体**：任意 Mesh（把 Cube 缩放成扁盒子即可）挂上材质，再添加 `CloudNoiseGenerator` 组件，指定 `noiseCompute` 与 `volumeTexture` 两个引用。
4. **调节外观**：材质面板上的参数见下表；细节层会自动以略偏转的方向滚动。

## 参数速查

| 参数 | 说明 |
| --- | --- |
| `_StepSize` | 每步前进的距离，越小越精细、开销越高 |
| `_MaxSteps` | 单条射线的最大步进次数 |
| `_DensityScale` | 整体密度系数 |
| `_DetailScale` | 高频细节对基础密度的侵蚀强度 |
| `_CloudColor` | 云的颜色 |
| `_Brightness` | 最终颜色亮度 |
| `_WindDir` | 基础层风向（XYZ） |
| `_WindSpeed` | 基础层滚动速度 |
| `_DetailSpeed` | 细节层滚动速度 |

## 注意事项

- 噪声贴图分辨率需为 8 的倍数（对应 Compute Shader 的 `[numthreads(8, 8, 8)]`），64、128 均可。
- 采样时对 UVW 做 `frac` 循环，因此风场滚动可以无缝衔接。
- 正交与透视相机均已适配（射线方向与起点的取法不同，见 shader 内注释）。
- 若忘记勾选 `Enable Random Write`，`CloudNoiseGenerator` 会在 Console 报错并给出提示。
