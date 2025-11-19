//using DataAccessLayer.Dbcontext;
//using DataAccessLayer.Repositories.Interfaces;
//using DomainAccessLayer.Models;
//using Microsoft.EntityFrameworkCore;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace DataAccessLayer.Repositories
//{
//    public class ManagerIngredentRepository : IManagerIngredentRepository
//    {

//        private readonly SapaFoRestRmsContext _context;

//        public ManagerIngredentRepository(SapaFoRestRmsContext context)
//        {
//            _context = context;
//        }
//        public async Task<bool> AddIngredient(Ingredient ingredient)
//        {
//            throw new NotImplementedException();
//        }

//        public async Task<bool> DeleteIngredentById(int ingredientId)
//        {
//            throw new NotImplementedException();
//        }

//        public async Task<IEnumerable<Ingredient>> getAllIngredent()
//        {
//            return await _context.Ingredients.ToListAsync();
//        }

//        public async Task<Ingredient> getIngredientById(int id)
//        {
//            throw new NotImplementedException();
//        }

//        public async Task<bool> UpdateIngredent(Ingredient ingredient)
//        {
//            throw new NotImplementedException();
//        }
//    }
//}
