
using domain.Dto;
using domain.Pojo.protocol;
using domain.Records;
using infrastructure.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace dataPointsModule.Bll;

public interface IModbusDataBll
{
    /// <summary>
    /// 保存数据点
    /// </summary>
    /// <param name="modbusDataPoint"></param>
    public void Save(ModbusDataPoint modbusDataPoint);

    /// <summary>
    /// 删除数据点
    /// </summary>
    /// <param name="ids"></param>
    public void Remove(List<long> ids);

    
    public Pager<ModbusDataPoint> GetList(ModbusDataQuery query);

    public ModbusDataPoint Get(long id);


    public DataPointDto ReadById(long id);
    public DataPointDto ReadByName(string name);
    public DataPointDto WriteByName(string name, object value);
    public DataPointDto WriteById(long id, object value);

    public FileStreamResult ExportExcel(ModbusDataQuery query);

    public void ImportExcel(IFormFile file);
    


}