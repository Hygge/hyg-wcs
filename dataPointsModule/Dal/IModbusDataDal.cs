using domain.Dto;
using domain.Pojo.protocol;
using domain.Records;
using infrastructure.Utils;

namespace dataPointsModule.Dal;

public interface IModbusDataDal
{

    Pager<ModbusDataPoint> pager(ModbusDataQuery query);
    
    void Insert(ModbusDataPoint modbusDataPoint);
    
    ModbusDataPoint SelectByName(string name);

    void Delete(List<long> ids);

    ModbusDataPoint SelectById(long id);

    void BatchInsert(List<ModbusDataPoint> points);

    List<ModbusPointDto> SelectList(ModbusDataQuery query);

    void Update(ModbusDataPoint point);


}