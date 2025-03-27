using SqlSugar;

namespace domain.Pojo.wms;


/// <summary>
/// 容器
/// </summary>
public class Container
{
    [SugarColumn(IsPrimaryKey = true)]
    public string id { set; get; }
    
    /// <summary>
    /// 容器类型id
    /// </summary>
    public string ContainerTypeId { set; get; }
    
    /// <summary>
    /// 物料条码
    /// </summary>
    public string? materailNo { set; get; }
    
}