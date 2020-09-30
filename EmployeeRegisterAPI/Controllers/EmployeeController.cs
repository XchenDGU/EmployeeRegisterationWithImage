using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EmployeeRegisterAPI.Models;
using System.IO;
using Microsoft.AspNetCore.Hosting;

namespace EmployeeRegisterAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmployeeController : ControllerBase
    {
        private readonly EmployeeDbContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;

        public EmployeeController(EmployeeDbContext context,IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            this._hostEnvironment = hostEnvironment;
        }

        // GET: api/Employee
        [HttpGet]
        public async Task<ActionResult<IEnumerable<EmployeeModel>>> GetEmployees()
        {
            return await _context.Employees.ToListAsync();
        }

        [HttpGet("images")]
        public async Task<IActionResult> GetImages(String imageName)
        {

            var image = System.IO.File.OpenRead(Path.Combine(_hostEnvironment.ContentRootPath, "Images", imageName));
            return await Task.Run(() =>File(image, "image/jpeg"));
        }

        // GET: api/Employee/5
        [HttpGet("{id}")]
        public async Task<ActionResult<EmployeeModel>> GetEmployeeModel(int id)
        {
            var employeeModel = await _context.Employees.FindAsync(id);

            if (employeeModel == null)
            {
                return NotFound();
            }

            return employeeModel;
        }

        // PUT: api/Employee/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for
        // more details, see https://go.microsoft.com/fwlink/?linkid=2123754.
        [HttpPut("{id}")]
        public async Task<ActionResult<EmployeeModel>> PutEmployeeModel(int id, [FromForm]EmployeeModel employeeModel)
        {
            if (id != employeeModel.EmployeeId)
            {
                return BadRequest();
            }
            if(employeeModel.ImageFile != null)
            {
                //delete old image and set new image
                await DeleteImage(employeeModel.ImageName);
                employeeModel.ImageName = await SaveImage(employeeModel.ImageFile);
            }
            //employeeModel.ImageName = await SaveImage(employeeModel.ImageFile);
            
            _context.Entry(employeeModel).State = EntityState.Modified;

            //Don't need the file data after it's saved
            employeeModel.ImageFile = null;
            try
            {
                await _context.SaveChangesAsync();
                return employeeModel;
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!EmployeeModelExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
        }

        // POST: api/Employee
        // To protect from overposting attacks, enable the specific properties you want to bind to, for
        // more details, see https://go.microsoft.com/fwlink/?linkid=2123754.
        [HttpPost]
        public async Task<ActionResult<EmployeeModel>> PostEmployeeModel([FromForm]EmployeeModel employeeModel)
        {
            try
            {
                employeeModel.ImageName = await SaveImage(employeeModel.ImageFile);
                _context.Employees.Add(employeeModel);
                await _context.SaveChangesAsync();
                employeeModel.ImageFile = null;
                
                return CreatedAtAction("GetEmployeeModel", new { id = employeeModel.EmployeeId }, employeeModel);
            }catch(Exception e)
            {
                throw e;
            }
            
        }

        // DELETE: api/Employee/5
        [HttpDelete("{id}")]
        public async Task<ActionResult<EmployeeModel>> DeleteEmployeeModel(int id)
        {
            var employeeModel = await _context.Employees.FindAsync(id);
            if (employeeModel == null)
            {
                return NotFound();
            }

            await DeleteImage(employeeModel.ImageName);
            _context.Employees.Remove(employeeModel);
            await _context.SaveChangesAsync();

            return employeeModel;
        }

        private bool EmployeeModelExists(int id)
        {
            return _context.Employees.Any(e => e.EmployeeId == id);
        }

        [NonAction]
        public async Task<string> SaveImage(IFormFile imageFile)
        {
            string imageName = new String(Path.GetFileNameWithoutExtension(imageFile.FileName).Take(10).ToArray()).Replace(' ','-');
            imageName = imageName + DateTime.Now.ToString("yymmssfff") + Path.GetExtension(imageFile.FileName);
            var imagePath = Path.Combine(_hostEnvironment.ContentRootPath,"Images",imageName);
            using(var fileStream = new FileStream(imagePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(fileStream);
            }

            return imageName;

        }

        [NonAction]
        public async Task<Boolean> DeleteImage(String imageName)
        {
            //string imageName = new String(Path.GetFileNameWithoutExtension(imageFile.FileName).Take(10).ToArray()).Replace(' ', '-');
            //imageName = imageName + DateTime.Now.ToString("yymmssfff") + Path.GetExtension(imageFile.FileName);
            var imagePath = Path.Combine(_hostEnvironment.ContentRootPath, "Images", imageName);
            if (System.IO.File.Exists(imagePath))
            {
                await Task.Run(()=>System.IO.File.Delete(imagePath));
                return true;
            }
            return false;
        }
    }
}
