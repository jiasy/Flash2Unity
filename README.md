Flash2Unity
Flash动画以及帧信息输出成json，并在Unity端解析json显示Flash编辑的动画内容。
使用方法
	1.复制 Assets/Flash2Unity/FLAs/Empty 文件夹，保持文件路径结构(如 Assets/Flash2Unity/FLAs/Easy)
	2.打开 Fla 文件，以
		1).Properties -> Publish -> Script -> 扳手按钮(点击弹出界面) -> SWC and ANE files or folders
			确保，Assets/Flash2Unity/FLAs/analyse.swc，Assets/Flash2Unity/FLAs/serviceApp.swc
        2).Properties -> Publish -> Class 
            确保其继承自 SwfAnalyseMain
        3).支持Sprite和MovieClip，由于命名冲突的原因，在Flash内，Sprite对应于Pic,MovieClip对应于Dis
            3-1.Sprite:
                必须是继承于 Pic 的 MovieClip
					其内的第一帧用来输出图片，第二帧可以用来保留制作过程中的矢量图
					第一帧转位图之后，需要居中对齐，否则报错。
				命名规则 altas_picName [大图名_图片名]
				不支持翻转，用继承于Dis的MovieClip包裹Pic实现XY轴翻转效果
					翻转的Pic必须使用 Flipx_instanceName [Flip(X或Y轴)_实例名称]
					只翻转scaleX = -1，不要动旋转等属性，否者报错。
			3-2.MovieClip
				必须继承于 Dis 的 MovieClip
					转换到 Unity 之后，类名依旧是MovieClip
					在 Unity 侧，支持nextFrame(),gotoAndPlay(x),stop()等AS3的API
					在 Unity 侧，每一个 MovieClip 都持有 Sorting Group 组件，确保层级和显示一致
					支持帧名，也支持帧名中的特定方法调用
						帧名:没有 '.()' 其中任意字符的，会别当做帧名来处理
						方法:用.分割前面为显示路径，后面为执行方法
							方法支持nextFrame(),stop()等等
								例如:frog.gtp(2) 代表名称为frog的MovieClip去播放第二帧。
	3.放置于场景内的继承自 Dis 的 MovieClip，才会被解析成 json
		1).Flash使用快捷 ctrl+Enter 发布 Air 程序。
		2).指定工程文件，右下角的按钮
			注意，必须在Resources/flash文件夹中，需要手动创建
		3).将Flash结构转换到Unity中
			第一个按钮，生成图片和json文件
				图片就是所有场景上继承自Pic的MovieClip，其中第一帧的内容
			第二个按钮，只生成json文件
				如果，只是改动的动画，并没有改动图片的话，点击这个按钮
	4.Unity中的处理
		1).Assets/Flash2Unity/Scripts/Base/Editor/FlashAssetPostprocessor.cs
			当资源导入到 Resouces/flash 中时，会根据文件夹内容重新合并大图
		2).MovieClip 和 Sprite 继承自 DisplayObject
			2-1).DisplayObject 用来同步属性
			2-2).Sprite 用来显示图片
				图片不会反复创建，当从场景移除的时候，只是将其放置到 nodeNotOnStage 节点下隐藏起来。
			2-3).MovieClip 提供帧控制支持，模拟Flash中时间轴控制
		3).MovieClipContainer 用来在Editor中指明显示对象类名。
			根据 className 填写的类名解析相应json文件，并且加载到场景中。
		4).MovieClip 通过类名关联，添加 Unity 同名组件。
			如果Flash中继承自Dis的显示对象，其名称在Unity中也存在组件的话，创建MovieClip的时候会自动创建并挂载组件。
				通过名称关联的方式，可以减少配置文件(例如:/Assets/Flash2Unity/Scripts/Sample/Sample_DisBtn.cs)
		5).场景中一定要放置一个挂载 FlashManager 组件的节点
			运行时管理节点
			配置图片和json的路径
	5.Unity中文件结构
		1).Scenes 为事例场景
			1.Main 对应 Assets/Flash2Unity/FLAs/Flash2Unity/Flash2Unity.fla
				在场景摆放挂载MovieClipContainer组件的节点，来指定类名，显示Flash动画。
			2.CreateByCode 对应 Assets/Flash2Unity/FLAs/Flash2Unity/Flash2Unity.fla
				Assets/Flash2Unity/CreateByCode.cs 通过的代码创建Flash动画，并摆放其位置。
				通过类名指定要解析的json文件，并转换成动画显示到场景。
			3.Performance 对应 Assets/Flash2Unity/FLAs/Flash2Unity/Flash2Unity.fla
				Assets/Flash2Unity/Performance.cs 通过 animationNumber 指定个数，查看性能
			4.Easy 对应 Assets/Flash2Unity/FLAs/Easy (Scenes-4.Easy)/Easy.fla
				VideoTutorials 对应的源文件
		2).Flash2Unity
			1.FLAs                  : Flash源文件，注意Fla和swc的层级关系
			2.LitJson               : 第三方的 json 库
			3.Resources/flash/jsons : MovieClip的帧信息
			4.Resources/flash/pngs  : Sprite使用的图像文件
			5.Scripts/Base/Editor   : 编辑器支持
			6.Scripts/Base/flash    : AS3中关于Sprite和MovieClip在Unity侧的简单实现
			7.Scripts/Sample        : 支持 Flash2Unity.fla 所需要的代码
视频演示链接
https://www.bilibili.com/video/av73034285?p=1
		
	
		
		
			
	
		
				
		
