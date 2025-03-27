using domain.Pojo.baseEntity;
using SqlSugar;

namespace domain.Pojo.wms;

/// <summary>
/// 容器类型
/// </summary>
public class ContainerType : AduitWithUser
{
    [SugarColumn(IsPrimaryKey = true)]
    public string id { set; get; }

    /// <summary>
    /// 容器类型名称
    /// </summary>
    public string name { set; get; } = string.Empty;
    
}