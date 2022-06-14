using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
  [EnableCors("Public")]
  /* no longer need because of inheritance from BaseApiController
  [ApiController]
  [Route("api/[controller]")]
  */
  [Authorize]
  public class UsersController : BaseApiController
  {
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;

    //private readonly DataContext _context;
    public UsersController(IUserRepository userRepository, IMapper mapper)
    {
      _mapper = mapper;
      _userRepository = userRepository;
      //_context = context;
    }

    [HttpGet]
    //[AllowAnonymous]
    public async Task<ActionResult<IEnumerable<MemberDto>>> GetUsers()
    {
      //return await _context.Users.ToListAsync();
      //can do .Result as an alternative but does not wait
      //return Ok(await _userRepository.GetUsersAsync());
      //when using AppUser

      /*       var users = await _userRepository.GetUsersAsync();
            var usersToReturn = _mapper.Map<IEnumerable<MemberDto>>(users);
            return Ok(usersToReturn); */

      var users = await _userRepository.GetMembersAsync();
      return Ok(users);
    }
    // api/users/# # = id of user
    [Authorize]
    [HttpGet("{username}")]
    public async Task<ActionResult<MemberDto>> GetUser(string username)
    {
      return await _userRepository.GetMemberAsync(username);
      //instead of GetUserByUsername Async
      //var user = await _userRepository.GetUserByUsernameAsync(username);
      //return _mapper.Map<MemberDto>(user);
      //automapper will map from MemberDto to Appuser
    }

    [HttpPut]
    public async Task<ActionResult> UpdateUser(MemberUpdateDto memberUpdateDto){
      var username = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
      var user = await _userRepository.GetUserByUsernameAsync(username);
      _mapper.Map(memberUpdateDto, user); //automapper so you don't have to go through all 
      _userRepository.Update(user);

      if (await _userRepository.SaveAllAsync()) return NoContent();
      return BadRequest("Failed to update user");
    }
  }
}