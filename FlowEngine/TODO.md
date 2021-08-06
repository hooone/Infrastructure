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

TODO
单元测试并发锁
触发器
流程测试
Runtime超时判断
日志规范化
提示规范化
执行过程中避免查配置库，缓存所有配置
采用单线程模型，所有外部操作在Command中实现异步