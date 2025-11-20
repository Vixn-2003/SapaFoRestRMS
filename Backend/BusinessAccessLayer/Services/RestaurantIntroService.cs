using BusinessAccessLayer.DTOs.RestaurantIntroDto;
using BusinessAccessLayer.Services.Interfaces;
using DataAccessLayer.Repositories.Interfaces;
using DomainAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessAccessLayer.Services
{
    public class RestaurantIntroService : IRestaurantIntroService
    {
        private readonly IRestaurantIntroRepository _repo;
        private readonly ICloudinaryService _cloudinary;

        public RestaurantIntroService(IRestaurantIntroRepository repo, ICloudinaryService cloudinary)
        {
            _repo = repo;
            _cloudinary = cloudinary;
        }

        public async Task<IEnumerable<RestaurantIntroDto>> GetAllAsync()
        {
            var list = await _repo.GetAllAsync();
            return list.Select(x => new RestaurantIntroDto
            {
                IntroId = x.IntroId,
                Title = x.Title,
                Description = x.Description,
                ImageUrl = x.ImageUrl,
                VideoUrl = x.VideoUrl,
                IsActive = x.IsActive,
                CreatedDate = x.CreatedDate,
                UpdatedDate = x.UpdatedDate
            });
        }

        public async Task<IEnumerable<RestaurantIntroDto>> GetActiveAsync()
        {
            var list = await _repo.GetActiveAsync();
            return list.Select(x => new RestaurantIntroDto
            {
                IntroId = x.IntroId,
                Title = x.Title,
                Description = x.Description,
                ImageUrl = x.ImageUrl,
                VideoUrl = x.VideoUrl,
                IsActive = x.IsActive
            });
        }

        public async Task<RestaurantIntroDto?> GetByIdAsync(int id)
        {
            var intro = await _repo.GetByIdAsync(id);
            if (intro == null) return null;

            return new RestaurantIntroDto
            {
                IntroId = intro.IntroId,
                Title = intro.Title,
                Description = intro.Description,
                ImageUrl = intro.ImageUrl,
                VideoUrl = intro.VideoUrl,
                IsActive = intro.IsActive
            };
        }

        public async Task<RestaurantIntroDto> CreateAsync(CreateRestaurantIntroDto dto)
        {
            string? imageUrl = null;
            if (dto.Image != null)
                imageUrl = await _cloudinary.UploadImageAsync(dto.Image, "restaurant_intro");

            var intro = new RestaurantIntro
            {
                Title = dto.Title,
                Description = dto.Description,
                ImageUrl = imageUrl,
                VideoUrl = dto.VideoUrl,
                IsActive = dto.IsActive,
                CreatedBy = dto.CreatedBy,
                CreatedDate = DateTime.Now
            };

            await _repo.AddAsync(intro);
            await _repo.SaveChangesAsync();

            return new RestaurantIntroDto
            {
                IntroId = intro.IntroId,
                Title = intro.Title,
                Description = intro.Description,
                ImageUrl = intro.ImageUrl,
                VideoUrl = intro.VideoUrl,
                IsActive = intro.IsActive
            };
        }

        public async Task<bool> UpdateAsync(int id, UpdateRestaurantIntroDto dto)
        {
            var intro = await _repo.GetByIdAsync(id);
            if (intro == null) return false;

            if (dto.Image != null)
            {
                if (!string.IsNullOrEmpty(intro.ImageUrl))
                    await _cloudinary.DeleteImageAsync(intro.ImageUrl);

                intro.ImageUrl = await _cloudinary.UploadImageAsync(dto.Image, "restaurant_intro");
            }

            intro.Title = dto.Title ?? intro.Title;
            intro.Description = dto.Description ?? intro.Description;
            intro.VideoUrl = dto.VideoUrl ?? intro.VideoUrl;
            intro.IsActive = dto.IsActive ?? intro.IsActive;
            intro.UpdatedDate = DateTime.Now;

            await _repo.UpdateAsync(intro);
            await _repo.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var intro = await _repo.GetByIdAsync(id);
            if (intro == null) return false;

            if (!string.IsNullOrEmpty(intro.ImageUrl))
                await _cloudinary.DeleteImageAsync(intro.ImageUrl);

            await _repo.DeleteAsync(intro);
            await _repo.SaveChangesAsync();
            return true;
        }
    }
}
