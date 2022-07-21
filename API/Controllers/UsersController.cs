using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
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
    private readonly IPhotoService _photoService;

    //private readonly DataContext _context;
    public UsersController(IUserRepository userRepository, IMapper mapper, IPhotoService photoService)
    {
      _photoService = photoService;
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
    [HttpGet("{username}", Name = "GetUser")]
    public async Task<ActionResult<MemberDto>> GetUser(string username)
    {
      return await _userRepository.GetMemberAsync(username);
      //instead of GetUserByUsername Async
      //var user = await _userRepository.GetUserByUsernameAsync(username);
      //return _mapper.Map<MemberDto>(user);
      //automapper will map from MemberDto to Appuser
    }

    [HttpPut]
    public async Task<ActionResult> UpdateUser(MemberUpdateDto memberUpdateDto)
    {
      //var username = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
      var username = User.GetUsername();
      var user = await _userRepository.GetUserByUsernameAsync(username);
      _mapper.Map(memberUpdateDto, user); //automapper so you don't have to go through all 
      _userRepository.Update(user);

      if (await _userRepository.SaveAllAsync()) return NoContent();
      return BadRequest("Failed to update user");
    }
    [HttpPost("add-photo")]
    public async Task<ActionResult<PhotoDto>> AddPhoto(IFormFile file)
    {
      var user = await _userRepository.GetUserByUsernameAsync(User.GetUsername());
      var result = await _photoService.AddPhotoAsync(file);

      if (result.Error != null) return BadRequest(result.Error.Message);

      var photo = new Photo
      {
        Url = result.SecureUrl.AbsoluteUri,
        PublicId = result.PublicId
      };

      if (user.Photos.Count == 0)
      {
        photo.IsMain = true;
      }
      user.Photos.Add(photo);

      if (await _userRepository.SaveAllAsync())
      {
        //return _mapper.Map<PhotoDto>(photo);
        return CreatedAtRoute("GetUser", new { username = user.UserName }, _mapper.Map<PhotoDto>(photo));
      }

      return BadRequest("Problelm: Photo was not added");
    }
    [HttpPut("set-main-photo/{photoId}")]
    public async Task<ActionResult> SetMainPhoto(int photoId)
    {
      var user = await _userRepository.GetUserByUsernameAsync(User.GetUsername());
      var photo = user.Photos.FirstOrDefault(x => x.Id == photoId);

      if (photo.IsMain) return BadRequest("This is already your main photo");

      var currentMain = user.Photos.FirstOrDefault(x => x.IsMain);
      if (currentMain != null) currentMain.IsMain = false;
      photo.IsMain = true;

      if (await _userRepository.SaveAllAsync()) return NoContent();

      return BadRequest("Failed to set main photo");
    }
    [HttpPut("set-main-photo-false/{photoId}")]
    public async Task<ActionResult> SetMainPhotoFalse(int photoId)
    {
      var user = await _userRepository.GetUserByUsernameAsync(User.GetUsername());
      var photo = user.Photos.FirstOrDefault(x => x.Id == photoId);

      //if (photo.IsMain) return BadRequest("This is already your main photo");

      var currentMain = user.Photos.FirstOrDefault(x => x.IsMain);
      //if (currentMain != null) currentMain.IsMain = false;
      photo.IsMain = false;

      if (await _userRepository.SaveAllAsync()) return NoContent();

      return BadRequest("Didn't set photo back to false");
    }
  }

}