﻿using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ParkiAPI.Data;
using ParkiAPI.Models;
using ParkiAPI.Repository.IRepository;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace ParkiAPI.Repository
{
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext dbContext;
        private readonly AppSettings appSettings;
        public UserRepository(ApplicationDbContext dbContext, IOptions<AppSettings> appSettings)
        {
            this.dbContext = dbContext;
            this.appSettings = appSettings.Value;
        }
        public User Authenticate(string username, string password)
        {
            var user = this.dbContext.Users.SingleOrDefault(x => x.UserName == username && x.Password == password);
            //user not found
            if (user == null)
            {
                return null;
            }
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(this.appSettings.Secret);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[] {
                    new Claim(ClaimTypes.Name, user.Id.ToString()),
                    new Claim(ClaimTypes.Role, user.Role)
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials
                                    (new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            user.Token = tokenHandler.WriteToken(token);
            user.Password = ""; //for the security, we do not need to send the password after the token has been created ~Burak
            return user;
        }

        public bool IsUniqueUser(string username)
        {
            var user = this.dbContext.Users.SingleOrDefault(x => x.UserName == username);

            // if user is null there isn't named user in database
            if(user == null)
            {
                return true;
            }
            return false;
        }

        public User Register(string username, string password)
        {
            User userObje = new User()
            {
                UserName = username,
                Password = password,
                Role ="Admin"
            };
            this.dbContext.Add(userObje);
            this.dbContext.SaveChanges();
            userObje.Password = "";
            return userObje;
        }
    }
}
