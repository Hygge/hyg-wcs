using domain.Pojo.baseEntity;
using SqlSugar;

namespace domain.Pojo.wms;

/// <summary>
/// 库区
/// </summary>
public class WarehouseArea : AduitWithUser
{
    [SugarColumn(IsPrimaryKey = true)]
    public string id { set; get; }

    /// <summary>
    /// 库区名称
    /// </summary>
    public string warehouseAreaName { set; get; } = string.Empty;
    
    /// <summary>
    /// 仓库id
    /// </summary>
    public string warehouseId { set; get; }
    
    /// <summary>
    /// 备注
    /// </summary>
    public string? remark { set; get; }
    
    
 
}