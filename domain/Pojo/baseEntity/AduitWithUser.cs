namespace domain.Pojo.baseEntity;

/// <summary>
/// 操作记录
/// </summary>
public class AduitWithUser
{
    public string? createdBy { set; get; }
    
    public string? updateBy { set; get; }
    
    public DateTime createdTime { set; get; } = DateTime.Now;
    
    public DateTime? updateTime { set; get; }
}