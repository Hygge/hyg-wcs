using System.Collections.Concurrent;
using System.Text;
using dataPointsModule.Attributes;
using dataPointsModule.Managers.Base;
using domain.Dto;
using domain.Enums;
using domain.Pojo.protocol;
using infrastructure.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using S7.Net;

namespace dataPointsModule.Managers;

/// <summary>
/// S7长连接管理
/// </summary>
[Service(ServiceLifetime.Singleton)]
public class S7Manager : ManagerAbstract<S7DataPoint>, IS7Manager
{
    public readonly ConcurrentDictionary<string, Plc> S7Online = new();
    private readonly ILogger<S7Manager> logger;
    private object s7Lock = new();
    
    public S7Manager(ILogger<S7Manager> logger) 
    {
        this.logger = logger;
    }
    
    public override void Connect(S7DataPoint t)
    {
        try
        {
            Plc plc = new Plc(getCpuType(t.cpuType), t.ip, t.port, Convert.ToInt16(t.rack), Convert.ToInt16(t.slot));
            plc.Open();
            bool b = S7Online.TryAdd(getPlcKey(t), plc);
            if (b)
                logger.LogInformation($"S7协议plc连接成功，IP：{t.ip}");
            else
                plc.Close();
           
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"S7协议plc连接失败，IP：{t.ip}，原因：{ex.Message}");
        }
    }

    public override void Disconnect(S7DataPoint t)
    {
        try
        {
            bool b = S7Online.Remove(getPlcKey(t), out Plc plc);
            if (b)
            {
                plc.Close();
                logger.LogInformation($"S7协议plc断开连接成功，IP：{t.ip}");
            }               
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"S7协议plc断开连接失败 IP：{t.ip}，原因：{ex.Message}");
        }
    }

    public override void Reconnect(S7DataPoint t)
    {
        try
        {
            logger.LogInformation($"S7协议plc正在重新连接，IP：{t.ip}");

            bool b = S7Online.TryGetValue(getPlcKey(t), out Plc plc);
            if (b)
            {
                Disconnect(t);
                return;
            }
            Connect(t);

        }
        catch (Exception ex) 
        {
            logger.LogInformation(ex, $"S7协议plc正在重新连接异常，IP：{t.ip}，原因：{ex.Message}");
        }
    }

    public override object GetDevice(S7DataPoint t)
    {
        try
        {
            lock (s7Lock)
            {
                // 存在并已连接则获取
                // 反之重新连接  
                bool b = S7Online.TryGetValue(getPlcKey(t), out Plc plc);
                if (b && plc.IsConnected)
                {
                    return plc;
                }
                Reconnect(t);
                return null;
            }  
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"S7协议在线plc连接获取失败，原因：{ex.Message}");
        }
        return null;
    }

    /// <summary>
    /// 获取在线存储key
    /// </summary>
    /// <param name="t"></param>
    /// <returns></returns>
    private string getPlcKey(S7DataPoint t)
    {
        return $"{t.ip}:{t.port}";
    }

    /// <summary>
    /// 获取cpu类型
    /// </summary>
    /// <param name="s"></param>
    /// <returns></returns>
    private CpuType getCpuType(string s)
    {
        switch (s)
        {
            case "S7200":
                return CpuType.S7200;
            case "S7300":
                return CpuType.S7300;
            case "S7400":
                return CpuType.S7400;
            case "S7200Smart":
                return CpuType.S7200Smart;
            case "S71200":
                return CpuType.S71200;
            case "S71500":
                return CpuType.S71500;
            default:
                return CpuType.S71200;
        }
    }
    
    
    [ProtocolLog(OperateEnum.Read, ProtocolEnum.S7)]
    public DataPointDto Read(S7DataPoint point)
    {
        Plc? plc  = GetDevice(point) as Plc;
        if (plc == null)
        {
            return new DataPointDto(false, null, $"S7协议plc不在线，IP：{point.ip}");
        }
        object? res = null;
        try
        {
            if (VarType.S7String.ToString().Equals(point.dataType))
            {
                byte[] bytes = plc.ReadBytes(DataType.DataBlock, point.db, point.startAddress, point.length);
                res = byteToStr(bytes);
            }else if (VarType.String.ToString().Equals(point.dataType))
            {
                byte[] bytes = plc.ReadBytes(DataType.DataBlock, point.db, point.startAddress, point.length);
                res = BytesToChar(bytes);
            }
            else
            {
                res = plc.Read(DataType.DataBlock, point.db, point.startAddress, getVarType(point.dataType),
                    point.length, Convert.ToByte(point.bit));
            }
            return new DataPointDto(true, res, string.Empty);
        }
        catch (Exception e)
        {
            return new DataPointDto(false, res, e.Message);
        }
    }

    /// <summary>
    /// 单个数据点写入
    /// </summary>
    /// <param name="point"></param>
    /// <param name="obj"></param>
    /// <returns></returns>
    [ProtocolLog(OperateEnum.Write, ProtocolEnum.S7)]
    public DataPointDto Write(S7DataPoint point, object obj)
    {
        Plc? plc  = GetDevice(point) as Plc;
        if (plc == null)
        {
            return new DataPointDto(false, null, $"S7协议plc不在线，IP：{point.ip}");
        }
        try
        {
            if (VarType.S7String.ToString().Equals(point.dataType))
            {
                plc.WriteBytes(DataType.DataBlock, point.db, point.startAddress, strToBytes(Convert.ToString(obj), point.length));
            }else if (VarType.String.ToString().Equals(point.dataType))
            {
                plc.WriteBytes(DataType.DataBlock, point.db, point.startAddress, charToBytes(Convert.ToString(obj), point.length));
            }
            else
            {
                plc.Write(DataType.DataBlock, point.db, point.startAddress, valueToVarType(obj, point.dataType));
            }
            return new DataPointDto(true, obj, string.Empty);
        }
        catch (Exception e)
        {
            return new DataPointDto(false, obj, e.Message);
        }
    }

    /// <summary>
    /// 字符数组转字节
    /// </summary>
    /// <param name="c"></param>
    /// <param name="length"></param>
    /// <returns></returns>
    private byte[] charToBytes(string c, int length)
    {
        byte[] bs = new byte[length];
        Encoding.ASCII.GetBytes(c).CopyTo(bs, 0);
        return bs;
    }
    /// <summary>
    /// 字节数组转字符串（一般用于plc char数组）
    /// </summary>
    /// <param name="bs"></param>
    /// <returns></returns>
    private string BytesToChar(byte[] bs)
    {
        List<byte> list = new();
        foreach (var t in bs)
        {
            if (t != 0)
            {
                list.Add(t);
            }
        }
        return Encoding.ASCII.GetString(list.ToArray());
    }
    
    
    private object valueToVarType(object v, string dataType)
    {
        if (Enum.GetName(VarType.Int).Equals(dataType))
        {
            return Convert.ToInt32(v);
        }
        if (Enum.GetName(VarType.Word).Equals(dataType))
        {
            return Convert.ToInt16(v);
        }
        if (Enum.GetName(VarType.DWord).Equals(dataType))
        {
            return Convert.ToInt16(v);
        }
        if (Enum.GetName(VarType.DInt).Equals(dataType))
        {
            return Convert.ToInt32(v);
        }
        if (Enum.GetName(VarType.Real).Equals(dataType))
        {
            return (float) Convert.ToDouble(v);
        }
        if (Enum.GetName(VarType.LReal).Equals(dataType))
        {
            return Convert.ToDouble(v);
        }
        if (Enum.GetName(VarType.Byte).Equals(dataType))
        {
            return Convert.ToByte(v);
        }
        if (Enum.GetName(VarType.Bit).Equals(dataType))
        {
            return Convert.ToBoolean(v);
        }
        
        return v;
    }

    /// <summary>
    /// 获取数据点类型
    /// </summary>
    /// <param name="dataType"></param>
    /// <returns></returns>
    private VarType getVarType(string dataType)
    {
        return (VarType) Enum.Parse(typeof(VarType), dataType);
    }

    /// <summary>
    /// s7协议字符串单独转换plc才能显示
    /// </summary>
    /// <param name="str"></param>
    /// <param name="length"></param>
    /// <returns></returns>
    private byte[] strToBytes(string str, int length)
    {
        byte[] value = Encoding.ASCII.GetBytes(str);
        List<byte> list = new List<byte>(length);
        list.Add(Convert.ToByte(length));
        list.Add(Convert.ToByte(value.Length));
        list.AddRange(value.ToList());
        int current = length - value.Length - 1;
        for (int i = current; i < length; i++)
            list.Add(0x00);
        return list.ToArray();
    }
    /// <summary>
    /// 读取s7字节转字符串
    /// </summary>
    /// <param name="bs"></param>
    /// <returns></returns>
    private string byteToStr(byte[] bs)
    {
        byte[] subArray = bs.Skip(2).Take(bs[0]).ToArray();
        List<byte> list = new();
        foreach (var t in subArray)
        {
            if (t != 0)
            {
                list.Add(t);
            }
        }
        return Encoding.ASCII.GetString(list.ToArray());
    }
    
}