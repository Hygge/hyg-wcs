namespace domain.Enums;

/// <summary>
/// 库位、物料、容器绑定关系
/// </summary>
public enum RelationTypeEnum
{
    /// <summary>
    /// 库位与容器
    /// </summary>
    Wlc = 0,
    /// <summary>
    /// 容器与物料
    /// </summary>
    Cm = 1,
    /// <summary>
    /// 库位与物料
    /// </summary>
    Wlm = 2,
    
}