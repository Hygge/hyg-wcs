# Hyg-WCS

概述：基于RBAC设计的.NET 通用后台管理系统，前后的分离项目，后端代码采用模块化开发，融合WMS系统相关业务



## 系统架构

### 后端

- dotnet8
- webApi
- sqlSugar
- 数据库 postgreSQL
- quartz

### 前端

- pnpm
- vue3
- pinia
- vue-router
- ant-design-vue ui


### 系统模块
- 系统模块
- 数据点维护模块 (S7、OpcUa、ModbusTcp)
- 基础设施模块
- 定时任务模块
- webApi启动模块


## 快速启动

### 前端

1. npm install -g pnpm  (安装pnpm)
2. pnpm init -y (初始化pnpm，有package-lock.json文件则不需要初始化)
3. pnpm dev (直接启动)

### 后端

1. 数据库初始化创建对应数据库名称，执行sql脚本在doc文件夹下（先导入init-postgresql.sql，再导入后面sql脚本），再导入后更新webApi\sql文件夹下
2. 安装dotnet8环境
3. 修改appsettings.json配置数据库连接
4. 直接启动后端 dotnet run

### 初始账户

1.演示账户： demo 123456

2.管理员账户：admin 123456


## 截图
![image](https://github.com/user-attachments/assets/bb8cb21f-846c-4825-924c-9c4bedaade59)

![image](https://github.com/user-attachments/assets/2d54fa36-7787-4e5e-bb8c-b32fbe467b85)

![image](https://github.com/user-attachments/assets/6e09a0ab-a0b8-4d1d-8dbd-94878fd22e38)

