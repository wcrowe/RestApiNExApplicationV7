using LazyCache;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RestApiNExApiV6.Domain;
using RestApiNExApiV6.Domain.Service;
using RestApiNExApiV6.Entity;
using RestApiNExApiV6.Entity.Context;
using RestApiNExApiV6.Entity.UnitofWork;
using Serilog;
using System.Collections.Generic;
using System.Linq;

namespace RestApiNExApiV6.Api.Controllers
{
    [ApiVersion("1.0")]
    [Route("api/[controller]")]
    //[Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IAppCache _lazyCache = new CachingService();
        private readonly UserService<UserViewModel, User> _userService;
        public UserController(UserService<UserViewModel, User> userService, IAppCache cache)
        {
            _userService = userService;
            _lazyCache = cache;
        }

        //get all
        [Authorize]
        [HttpGet]
        public IEnumerable<UserViewModel> GetAll()
        {
            var test = _userService.DoNothing();
            var items = _lazyCache.GetOrAdd($"Users", () => _userService.GetAll());
            return items;
        }

        //get all with pagination 
        [Authorize]
        [HttpGet("pagination/{page}/{quantityperpage}")]
        public IEnumerable<UserViewModel> GetAll(int page, int quantityperpage)
        {
            Pagination pagination = new Pagination { Page = page, QuantityPerPage = quantityperpage };
            var items = _userService.GetAll(pagination);
            HttpContext.Response.Headers.Add("pagesQuantity", _userService.PaginationPagesCnt.ToString());
            return items;
        }


        //get by predicate example
        //get all active by username
        [Authorize]
        [HttpGet("GetActiveByFirstName/{firstname}")]
        public IActionResult GetActiveByFirstName(string firstname)
        {
            var items = _lazyCache.GetOrAdd($"Users-{firstname}", () => _userService.Get(a => a.IsActive && a.FirstName == firstname));
            return Ok(items);
        }

        //get one
        [Authorize]
        [HttpGet("{id}")]
        public IActionResult GetById(int id)
        {
            var item = _lazyCache.GetOrAdd($"User-{id}", () => _userService.GetOne(id));
            if (item == null)
            {
                Log.Error("GetById({ ID}) NOT FOUND", id);
                return NotFound();
            }

            return Ok(item);
        }


        //add
        [Authorize(Roles = "Administrator")]
        [HttpPost]
        public IActionResult Create([FromBody] UserViewModel user)
        {
            if (user == null)
                return BadRequest();

            var id = _userService.Add(user);
            return Created($"api/User/{id}", id);  //HTTP201 Resource created
        }

        //update
        [Authorize(Roles = "Administrator")]
        [HttpPut("{id}")]
        public IActionResult Update(int id, [FromBody] UserViewModel user)
        {
            if (user == null || user.Id != id)
                return BadRequest();

            int retVal = _userService.Update(user);
            if (retVal == 0)
                return StatusCode(304);  //Not Modified
            else if (retVal == -1)
                return StatusCode(412, "DbUpdateConcurrencyException");  //412 Precondition Failed  - concurrency
            else
                return Accepted(user);
        }

        //delete
        [Authorize(Roles = "Administrator")]
        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            int retVal = _userService.Remove(id);
            if (retVal == 0)
                return NotFound();  //Not Found 404
            else if (retVal == -1)
                return StatusCode(412, "DbUpdateConcurrencyException");  //Precondition Failed  - concurrency
            else
                return NoContent();   	     //No Content 204
        }


        //use READ stored procedure example
        [Authorize]
        [HttpGet("GetUsersByName/{firstname}/{lastname}")]
        public IActionResult GetUsersByName(string firstname, string lastname)
        {
            var items = _userService.GetUsersByName(firstname, lastname);
            return Ok(items);
        }

        //use CUD stored procedure example
        [Authorize]
        [HttpGet("UpdateEmailbyUsername/{username}/{email}")]
        public IActionResult UpdateEmailbyUsername(string username, string email)
        {
            int id = _userService.UpdateEmailByUsername(username, email);
            return Ok(id);
        }


    }
}


