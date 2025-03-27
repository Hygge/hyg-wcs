using dataPointsModule.Bll;
using domain.Pojo.protocol;
using domain.Records;
using domain.Result;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace dataPointsModule.Controller;


[Authorize]
[ApiController]
[Route("[controller]/[action]")]
public class ModbusDataController : ControllerBase
{
    private IModbusDataBll _modbusDataBll;
    public ModbusDataController(IModbusDataBll modbusDataBll)
    {
        _modbusDataBll = modbusDataBll;
    }

    [HttpPost]
    public ApiResult list([FromBody] ModbusDataQuery query)
    {
        return ApiResult.succeed( _modbusDataBll.GetList(query));
    }
    
    [HttpPost]
    public ApiResult save([FromBody] ModbusDataPoint point)
    {
        _modbusDataBll.Save(point);
        return ApiResult.succeed();
    }

    [HttpDelete]
    public ApiResult del([FromBody] List<long> ids)
    {
        _modbusDataBll.Remove(ids);
        return ApiResult.succeed();
    }

    [HttpPost]
    public ApiResult readById(long id)
    {
        return ApiResult.succeed(_modbusDataBll.ReadById(id));
    }
    
    [HttpPost]
    public ApiResult writeById(long id, string value)
    {
        return ApiResult.succeed(_modbusDataBll.WriteById(id, value));
    }

    [HttpGet("{id}")]
    public ApiResult get([FromRoute] long id)
    {
        return ApiResult.succeed(_modbusDataBll.Get(id));
    }
    
    [HttpPost]
    public IActionResult export([FromBody] ModbusDataQuery query)
    {
        return _modbusDataBll.ExportExcel(query);
    }
    [HttpPost]
    public ApiResult import(IFormFile file)
    {
        _modbusDataBll.ImportExcel(file);
        return ApiResult.succeed();
    }
    
    


}