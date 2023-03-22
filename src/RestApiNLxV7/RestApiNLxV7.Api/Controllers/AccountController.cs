using Microsoft.AspNetCore.Mvc;
using RestApiNLxV7.Data;
using RestApiNLxV7.Data.Entities;
using System.Collections.Generic;

namespace RestApiNLxV7.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {

        private readonly IDataService _dataService;
        public AccountController(IDataService dataService)
        {
            _dataService = dataService;
        }


        [HttpGet("{accountId}")]
        public Account GetAccount(int accountId)
        {
            return _dataService.GetAccount(accountId);
        }

        [HttpGet]
        public IEnumerable<Account> GetAccounts()
        {
            return _dataService.GetAccounts();
        }

        [HttpPost]
        public IActionResult AddAccount([FromBody] Account account)
        {
            _dataService.AddAccount(account);
            return StatusCode(201);
        }

        [HttpPut]
        public IActionResult UpdateAccount([FromBody] Account account)
        {
            _dataService.UpdateAccount(account);
            return StatusCode(202);
        }

        [HttpDelete]
        public IActionResult DeleteAccount(int id)
        {
            _dataService.DeleteAccount(id);
            return StatusCode(204);
        }

    }
}
