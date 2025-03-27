using domain.Enums;

namespace domain.Pojo.wms;

/// <summary>
/// 库位容器绑定关系
/// </summary>
public class WarehouseLocationContainer
{
    /// <summary>
    /// 库位id
    /// </summary>
    public string? warehouseLocationId { set; get; }
    
    /// <summary>
    /// 容器id
    /// </summary>
    public string? containerId { set; get; }
    
    /// <summary>
    /// 物料条码
    /// </summary>
    public string? materailNo { set; get; }

    /// <summary>
    /// 关联关系类型
    /// </summary>
    public RelationTypeEnum relationType { set; get; } = RelationTypeEnum.Wlc;

}