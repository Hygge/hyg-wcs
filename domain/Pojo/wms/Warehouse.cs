using domain.Pojo.baseEntity;
using SqlSugar;

namespace domain.Pojo.wms;

/// <summary>
/// 仓库
/// </summary>
public class Warehouse  : AduitWithUser
{
    [SugarColumn(IsPrimaryKey = true)]
    public string id { set; get; }

    /// <summary>
    /// 仓库名称
    /// </summary>
    public string warehouseName { set; get; } = string.Empty;
    
    /// <summary>
    /// 备注
    /// </summary>
    public string? remark { set; get; }
    

}