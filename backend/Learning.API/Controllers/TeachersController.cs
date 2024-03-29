﻿using Learning.API.Data;
using Learning.API.Interface;
using Learning.API.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace Learning.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TeachersController : ControllerBase
    {
        private readonly TokenService _tokenService;
        private readonly AppDbContext _context;
        public TeachersController(AppDbContext context, TokenService tokenService)
        {
            _tokenService = tokenService;
            _context = context;
        }

        [HttpPost("register")]
        public async Task<ActionResult<TokenUser>> Register(AppUser user)
        {
            if (await TeacherExists(user.Email)) return BadRequest("Email is taken.");
            var hash = new HMACSHA512();
            var teacher = new Teacher
            {
                Name = user.Name,
                Email = user.Email,
                PasswordHash = hash.ComputeHash(Encoding.UTF8.GetBytes(user.Password)),
                PasswordSalt = hash.Key
            };
            /*var newTeacher = new Teacher
            {
                Name =teacher.Name,
                Email = teacher.Email,
                Password = hash.ComputeHash(Encoding.UTF8.GetBytes(teacher.Password))
            };*/
            //_context.Teachers.Add(teacher);
            _context.Teachers.Add(teacher);
            await _context.SaveChangesAsync();
            return new TokenUser
            {
                Email = teacher.Email,
                Token = _tokenService.CreateToken(teacher)
            };
        }

       [HttpPost("login")]
        public async Task<ActionResult<TokenUser>> Login([FromBody] AppUser user)
        {
            var teacher = await _context.Teachers.FirstOrDefaultAsync(t =>
            t.Email == user.Email);
            if (teacher == null) return Unauthorized("Invalid Email");
            var saltDecode = new HMACSHA512(teacher.PasswordSalt);
            var hashDecode = saltDecode.ComputeHash(Encoding.UTF8.GetBytes(user.Password));

            for (int i = 0; i < hashDecode.Length; i++)
            {
                if (hashDecode[i] != teacher.PasswordHash[i])
                {
                    return Unauthorized("Invalid password");
                }
            }

            return new TokenUser
            {
                Email = teacher.Email,
                Token = _tokenService.CreateToken(teacher)
            };
        }

        [HttpGet("{email}")]
        public async Task<ActionResult<List<Teacher>>> GetTeacher(string email)
        {
            var found = await _context.Teachers.FirstOrDefaultAsync(teacher => teacher.Email == email);
            if (found == null)
            {
                return BadRequest();
            }
            return Ok(found);
        }


        private async Task<bool> TeacherExists(string email)
        {
            return await _context.Teachers.AnyAsync(teacher => teacher.Email == email);
        }
    }
}