using LECOMS.Data.Models;
using LECOMS.RepositoryContract.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Repository.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly LecomDbContext _context;
        public IUserRepository Users { get; }
        public IShopRepository Shops { get; }
        public ICourseRepository Courses { get; }
        public ICourseSectionRepository Sections { get; }
        public ILessonRepository Lessons { get; }
        public ICourseProductRepository CourseProducts { get; }
        public ICourseCategoryRepository CourseCategories { get; }
        public IProductCategoryRepository ProductCategories { get; }
        public IProductRepository Products { get; }
        public IEnrollmentRepository Enrollments { get; }

        public IProductImageRepository ProductImages { get; }
        public ILessonProductRepository LessonProducts { get; }
        public ILandingPageRepository LandingPage { get; }

        public UnitOfWork(LecomDbContext context, IUserRepository userRepository, IShopRepository shopRepository, ICourseRepository courseRepo,
        ICourseSectionRepository sectionRepo,
        ILessonRepository lessonRepo,
        ICourseProductRepository cpRepo, ICourseCategoryRepository courseCategories, IProductCategoryRepository productCategories, IProductRepository products, IEnrollmentRepository enrollmentRepository, IProductImageRepository productImageRepository, ILessonProductRepository lessonProductRepository, ILandingPageRepository landingPage)
        {
            _context = context;
            Users = userRepository;
            Shops = shopRepository;

            Courses = courseRepo;
            Sections = sectionRepo;
            Lessons = lessonRepo;
            CourseProducts = cpRepo;
            CourseCategories = courseCategories;
            ProductCategories = productCategories;
            Products = products;
            Enrollments = enrollmentRepository;
            ProductImages = productImageRepository;
            LessonProducts = lessonProductRepository;
            LandingPage = landingPage;
        }
        public async Task<int> CompleteAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task<IDbContextTransaction> BeginTransactionAsync()
        {
            return await _context.Database.BeginTransactionAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
