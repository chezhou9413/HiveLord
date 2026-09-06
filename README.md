# LEVIATHANS: HIVE LORD

[English](README.en.md)

RimWorld 1.6 的霸王虫战斗模组，包含辛迪加狩猎委托、孢子喷涌虫和可学习的召唤技能。提供 Windows、macOS 和 Linux 桌面资源包，依赖 Harmony 与 ChezhouLib。

## 目录

| 路径 | 职责 |
| --- | --- |
| `HiveLord/About` | 模组元数据、封面与图标 |
| `HiveLord/1.6/Defs` | 实体、技能、任务和资源注册定义 |
| `HiveLord/1.6/Source/HiveLordLib` | 游戏运行代码 |
| `HiveLord/1.6/Languages` | 英文与简体中文语言资源 |
| `HiveLord/1.6/AssetBundles` | Windows、macOS、Linux 模型和 Shader 资源包 |
| `HiveLord/Docs` | 玩法规则、接口与维护约定 |

可加载的模组根目录为 `HiveLord`，不是本仓库外层目录。Unity 素材工程位于 `E:\mygame\HiveLordRimWorld`。

## 玩法

霸王虫在地下追踪敌人，出土后喷酸或砸地，再钻回地下换位。默认生命值为24000，单次承受伤害上限为100。致死后立即停止攻击与受击，虫躯从当前埋深挣出地面，衔接砸地倒伏；实际触地时播放冲击尘浪和音效，再定格90 Tick并用600 Tick下沉。动画姿势之间以18 Tick过渡，阶段进度与落地反馈标记随存档保存，暂停时停止。模组设置可调整伤害、承伤、生命值、进攻频率和攻击预警。

殖民地总财富达到50万后，辛迪加会在有合法选址时发出一次狩猎委托。进入巢域，先用爆炸武器清除孢子源；3分钟后霸王虫出土。猎杀报酬为10000白银、1000超织物和召唤霸王虫训练器。

选址沿殖民地可通行路线搜索8–36格旅行距离，优先使用天然平坦干旱灌木林；没有天然场地时，选择同范围内可达、未占用的陆地。接受委托时复核路线，将最终场地改为干旱灌木林和平坦地形，并清除该格的地标及特殊地貌规则。改造格的年均温度设为26°C、降水量800毫米，沼泽度为0，同时刷新气候、世界显示及寻路缓存。地形改造随世界存档保留，重试复用同一据点。仅在附近完全没有合法可达空地时暂缓发放，不会因缺少天然平原锁定任务。

训练器永久教授召唤技能，无需灵能链接。召唤体效忠使用者阵营，自动猎杀地图上的敌人；体型与攻击范围减半、生命值为三分之一（默认8000），存在1天，技能冷却15天。

挑战的准备阶段和霸王虫战均计入原版对玩家的活动威胁判定。清完孢子源、暂时没有可攻击对象或霸王虫遁地，不会触发地块安全提示或提前开放安全重组远行队；仍可沿地图边缘撤离。任务结束后继续尊重原版对残存敌人的判定。普通地图中的存活敌对霸王虫开启战斗AI时也会保留地下威胁，友方召唤体不计入。

- [委托、重试与召唤规则](HiveLord/Docs/SyndicateHunt.md)
- [孢子喷涌虫与地图影响](HiveLord/Docs/SporeSpewer.md)
- [本地化维护约定](HiveLord/Docs/Localization.md)
- [挑战场景音乐](HiveLord/Docs/ChallengeMusic.md)

## 运行架构

`Things` 管理实体、受击区域与死亡；`Combat` 和 `Targeting` 负责攻击循环、命中和选点；`Rendering` 将独立 Unity 模型捕获为透明纹理，再合成到地图。虫体遮罩单独捕获，酸液粒子不参与轮廓识别。每个可见霸王虫实例拥有独立舞台，允许任务目标与召唤体共存。

`SporeSpewer` 管理固定建筑、孢子状态、地图雾和倒塌。存活虫体共用捕获结果，实例粒子单独播放。`Quests`、`World`、`Challenge` 分别负责委托发放、世界据点和地图内进度；`Summoning` 管理训练、落点验证与召唤体寿命。`UI` 根据当前语言测量提示高度。

战斗参数位于 `Defs/ThingDefs/HiveLord_ThingDefs.xml` 的 `HiveLordCombatExtension` 中。Def 名称、资源键和存档字段是程序标识，不随显示语言改变。

## 构建

在本目录执行：

```bat
compile_modSelf.bat Release
```

项目为 .NET Framework 4.8.1 类库。MSBuild 和依赖程序集路径由构建脚本及 `HiveLordLib.csproj` 指定；换机时需配置本机路径。输出为 `HiveLord/1.6/Assemblies/HiveLordLib.dll`。`deploy_modSelf.bat` 调用本机部署工具复制模组。

仅修改 C#、Defs 或语言文件时不需要重建资源包。模型或 Shader 变更后，在 Unity 工程更新捕获预制体，并分别执行 `RimWorldTools` 菜单下的 Windows、macOS、Linux AssetBundle 构建。三个平台使用相同资源标签，每个平台均输出霸王虫、孢子喷涌虫和共享 Shader 三个包。

构建脚本位于 Unity 工程的 `Assets/Editor/BuildAssetBundle.cs`。命令行入口分别为 `BuildAssetBundles.BuildAll`、`BuildAssetBundles.BuildMac`、`BuildAssetBundles.BuildLinux`，使用 Unity 2022.3.35f1c1 及对应平台构建模块。macOS 编译 Metal、OpenGL Core，Linux 编译 OpenGL Core、Vulkan。构建暂存于 `Library/HiveLordAssetBundles`，成功后发布到本模组 `1.6/AssetBundles`；Windows 文件保持无后缀，另两个平台分别使用 `_mac.ab`、`_linux.ab`。

`Defs/ChezhouLib/HiveLord_UnityAssets.xml` 为三类资源声明各平台路径。ChezhouLib 根据 `Application.platform` 选择对应包，玩家无需手动切换文件。跨平台交付以 Unity 脚本、Shader 和资源包构建通过为界限，macOS 与 Linux 的游戏内显示仍需在对应设备确认。

日常代码验收以 Release 编译为界限；不自动启动 RimWorld、进入 Unity Play Mode 或建立测试工程。编译结果不能代替游戏内的视觉与存档验收。

## 代码接口

- `HiveLordQuestUtility.TryOfferQuest()`：检查财富、选址与唯一性后发放委托。
- `HiveLordProjectionThing.SetCombatAiEnabled(bool)`：切换自动战斗。
- `HiveLordProjectionThing.SetForcedCombatTarget(Thing)`：指定目标，传入 `null` 解除。
- `HiveLordSummonUtility.Spawn(Pawn, IntVec3)`：验证落点并生成限时召唤体。
- `SporeSpewerSpawnUtility.Spawn(Map, IntVec3)`：验证占地并生成孢子喷涌虫。
