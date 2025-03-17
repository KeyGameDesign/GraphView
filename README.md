如果没有Newtonjson，那就在manifest.json下加上这句话
"com.unity.nuget.newtonsoft-json": "3.2.1",

编辑器代码文件夹下的GraphViewEditor这个文件夹放置在Asset/Editor下


运行时代码的GraphView这个文件夹放置在各自项目的代码位置路径即可，但是需要注意的是导出的json配置文件路径要改
路径相关的文件在Asset/Editor/GraphViewEditor/EditorPath这个代码里
