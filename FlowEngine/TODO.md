0805
节点属性增加数据类型字段 ok 
Command固有属性再生成节点时添加 ok
添加属性保证Name唯一 ok
节点删除时未同时删除其属性 ok
payload装箱和拆箱 ok
单元测试  ok

0806
引入AutoMapper ok
简化NewCommand ok
把所有GUID生成拉到FlowConfigService层 ok
流程测试 ok

0809
抽象出NoBranchCommand，将一进一出的流程节点标准化 ok
延迟cmd开发 ok
cmd添加是否可自定义属性标志位 ok
自定义属性删除功能 ok

TODO
属性删除按钮
触发器开发
触发器的单元测试按钮改为手动触发按钮
command规范化日志
单元测试按钮并发锁
Runtime超时判断
基于触发器的流程运行功能
提示规范化
执行过程中避免查配置库，缓存所有配置
EXE阻塞警告功能
逐步跟踪流程运行
采用单线程模型，所有外部操作在Command中实现异步