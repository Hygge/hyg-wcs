using domain.Pojo.baseEntity;
using SqlSugar;

namespace domain.Pojo.wms;

/// <summary>
/// 库位
/// </summary>
public class WarehouseLocation  : AduitWithUser
{
    [SugarColumn(IsPrimaryKey = true)]
    public string id { set; get; }

    /// <summary>
    /// 锁定
    /// </summary>
    public bool isLock { set; get; } = true;
    /// <summary>
    /// 禁用
    /// </summary>
    public bool isDisable { set; get; } = true;
    
    /// <summary>
    /// 库区id
    /// </summary>
    public string warehouseAreaId { set; get; }
    
    /// <summary>
    /// 备注
    /// </summary>
    public string? remark { set; get; }
    
    
  
}