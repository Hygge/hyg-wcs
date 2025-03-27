using System.Collections.Concurrent;
using System.Net;
using dataPointsModule.Attributes;
using dataPointsModule.Managers.Base;
using domain.Dto;
using domain.Enums;
using domain.Pojo.protocol;
using infrastructure.Attributes;
using IoTClient.Clients.Modbus;
using IoTClient.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace dataPointsModule.Managers;

/// <summary>
/// Modbus长连接管理
/// </summary>
[Service(ServiceLifetime.Singleton)]
public class ModbusManager : ManagerAbstract<ModbusDataPoint>, IModbusManager
{
    public readonly ConcurrentDictionary<string, ModbusTcpClient> ModbusOnline = new();
    private readonly ILogger<ModbusManager> logger;
    private object modbusLock = new();
    
    public ModbusManager(ILogger<ModbusManager> logger) 
    {
        this.logger = logger; 
    }

    public override void Connect(ModbusDataPoint t)
    {
        try
        {
            ModbusTcpClient client = new ModbusTcpClient(new IPEndPoint(IPAddress.Parse(t.ip), t.port), 1500,
                getEndianFormat(t.format));
            client.Open();
            if (client.Connected)
            { 
                bool b = ModbusOnline.TryAdd(getKey(t), client);
                if (b)
                    logger.LogInformation($"Modbus连接成功，IP：{t.ip} port:{t.port}");
                else
                {
                    client.Close();
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Modbus连接失败，IP：{t.ip} port:{t.port}，原因：{ex.Message}");
        }
    }

    private EndianFormat getEndianFormat(string format)
    {
        return (EndianFormat)Enum.Parse(typeof(EndianFormat), format);
    }

    public override void Disconnect(ModbusDataPoint t)
    {
        try
        {
            bool b = ModbusOnline.Remove(getKey(t), out ModbusTcpClient client);
            if (b)
            {
                client.Close();
                logger.LogInformation($"Modbus断开连接成功，IP：{t.ip} port:{t.port}");
            }  
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            throw;
        }
    }

    public override void Reconnect(ModbusDataPoint t)
    {
        try
        {
            logger.LogInformation($"Modbus正在重新连接，IP：{t.ip} port:{t.port}");

            bool b = ModbusOnline.TryGetValue(getKey(t), out ModbusTcpClient client);
            if (b)
            {
                Disconnect(t);
                return;
            }
            Connect(t);
        }
        catch (Exception ex) 
        {
            logger.LogInformation(ex, "Modbus正在重新连接异常，IP：{t.ip} port:{t.port}，原因：{ex.Message}");
        }
    }

    public override object GetDevice(ModbusDataPoint t)
    {
        try
        {
            lock (modbusLock)
            {
                bool b = ModbusOnline.TryGetValue(getKey(t), out ModbusTcpClient client);
                if (b && client.Connected)
                {
                    return client;
                }
                Reconnect(t);
                return null;
            }  
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Modbus连接获取失败，IP：{t.ip} port:{t.port}，原因：{ex.Message}");
        }
        return null;
    }

    [ProtocolLog(OperateEnum.Read, ProtocolEnum.ModbusTcp)]
    public DataPointDto Read(ModbusDataPoint t)
    {
        ModbusTcpClient client = GetDevice(t) as ModbusTcpClient;
        if (client == null)
        {
            return new DataPointDto(false, null, $"Modbus设备不在线，IP：{t.ip} port:{t.port}");
        }
        try
        {
            if (t.readOnly)
            {
                var retI = client.ReadDiscrete(t.startAddress, (byte)t.stationNo);
                return new DataPointDto(retI.IsSucceed, retI.Value, retI.Err);
            }
            switch (t.dataType)
            {
                case "Bool":
                    var retb = client.ReadCoil(t.startAddress, (byte)t.stationNo);
                    return new DataPointDto(retb.IsSucceed, retb.Value, retb.Err);
                case "Double":
                    var retd = client.ReadDouble(t.startAddress, (byte)t.stationNo);
                    return new DataPointDto(retd.IsSucceed, retd.Value, retd.Err);
                case "Float":
                    var retf = client.ReadFloat(t.startAddress, (byte)t.stationNo);
                    return new DataPointDto(retf.IsSucceed, retf.Value, retf.Err);
                case "Int16":
                    var retInt16 = client.ReadInt16(t.startAddress, (byte)t.stationNo);
                    return new DataPointDto(retInt16.IsSucceed, retInt16.Value, retInt16.Err);
                case "Int32":
                    var retInt32 = client.ReadInt32(t.startAddress, (byte)t.stationNo);
                    return new DataPointDto(retInt32.IsSucceed, retInt32.Value, retInt32.Err);
                case "Int64":
                    var retInt64 = client.ReadInt64(t.startAddress, (byte)t.stationNo);
                    return new DataPointDto(retInt64.IsSucceed, retInt64.Value, retInt64.Err);
                case "UInt16":
                    var retUInt16 = client.ReadUInt16(t.startAddress, (byte)t.stationNo);
                    return new DataPointDto(retUInt16.IsSucceed, retUInt16.Value, retUInt16.Err);
                case "UInt32":
                    var retUInt32 = client.ReadUInt32(t.startAddress, (byte)t.stationNo);
                    return new DataPointDto(retUInt32.IsSucceed, retUInt32.Value, retUInt32.Err);
                case "UInt64":
                    var retUInt64 = client.ReadUInt64(t.startAddress, (byte)t.stationNo);
                    return new DataPointDto(retUInt64.IsSucceed, retUInt64.Value, retUInt64.Err);
                default:
                    return new DataPointDto(false, string.Empty, "数据类型未知");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"modbus读取数据失败，ip：{t.ip}，原因：{ex.Message}");
            return DataPointDto.failed(ex.Message);
        }
    }
    
    

    [ProtocolLog(OperateEnum.Write, ProtocolEnum.ModbusTcp)]
    public DataPointDto Write(ModbusDataPoint t, object value)
    {
        ModbusTcpClient client = GetDevice(t) as ModbusTcpClient;
        if (client == null)
        {
            return new DataPointDto(false, null, $"Modbus设备不在线，IP：{t.ip} port:{t.port}");
        }
        try
        {
            switch (t.dataType)
            {
                case "Bool":
                    var retb = client.Write(t.startAddress.ToString(), (bool) value, (byte)t.stationNo);
                    return new DataPointDto(retb.IsSucceed, value, retb.Err);
                case "Double":
                    var retd = client.Write(t.startAddress.ToString(), (double) value, (byte)t.stationNo);
                    return new DataPointDto(retd.IsSucceed, value, retd.Err);
                case "Float":
                    var retf = client.Write(t.startAddress.ToString(), (float)value, (byte)t.stationNo);
                    return new DataPointDto(retf.IsSucceed, value, retf.Err);
                case "Int16":
                    var retInt16 = client.Write(t.startAddress.ToString(), (Int16) value,  (byte)t.stationNo);
                    return new DataPointDto(retInt16.IsSucceed, value, retInt16.Err);
                case "Int32":
                    var retInt32 = client.Write(t.startAddress.ToString(), (Int32) value, (byte)t.stationNo);
                    return new DataPointDto(retInt32.IsSucceed, value, retInt32.Err);
                case "Int64":
                    var retInt64 = client.Write(t.startAddress.ToString(), (Int64) value,  (byte)t.stationNo);
                    return new DataPointDto(retInt64.IsSucceed, value, retInt64.Err);
                case "UInt16":
                    var retUInt16 = client.Write(t.startAddress.ToString(), (UInt16) value,  (byte)t.stationNo);
                    return new DataPointDto(retUInt16.IsSucceed, value, retUInt16.Err);
                case "UInt32":
                    var retUInt32 = client.Write(t.startAddress.ToString(), (UInt32) value,  (byte)t.stationNo);
                    return new DataPointDto(retUInt32.IsSucceed, value, retUInt32.Err);
                case "UInt64":
                    var retUInt64 = client.Write(t.startAddress.ToString(), (UInt64) value,  (byte)t.stationNo);
                    return new DataPointDto(retUInt64.IsSucceed, value, retUInt64.Err);
                default:
                    return new DataPointDto(false, string.Empty, "数据类型未知");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"modbus写入数据失败，ip：{t.ip}，原因：{ex.Message}");
            return DataPointDto.failed(ex.Message);
        }
    }

    private string getKey(ModbusDataPoint t)
    {
        return $"{t.ip}:{t.port}";
    }


}