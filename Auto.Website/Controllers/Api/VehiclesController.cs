﻿using System;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using Auto.Data;
using Auto.Data.Entities;
using Auto.Website.Models;
using Castle.Core.Internal;
using Microsoft.AspNetCore.Mvc;


namespace Auto.Website.Controllers.Api {
    [Route("api/[controller]")]
	[ApiController]
	public class VehiclesController : ControllerBase {
		private readonly IAutoDatabase db;

		public VehiclesController(IAutoDatabase db) {
			this.db = db;
		}

		private dynamic Paginate(string url, int index, int count, int total) {
			dynamic links = new ExpandoObject();
			links.self = new { href = url };
			links.final = new { href = $"{url}?index={total - (total % count)}&count={count}" };
			links.first = new { href = $"{url}?index=0&count={count}" };
			if (index > 0) links.previous = new { href = $"{url}?index={index - count}&count={count}" };
			if (index + count < total) links.next = new { href = $"{url}?index={index + count}&count={count}" };
			return links;
		}

		// GET: api/vehicles
		[HttpGet]
		[Produces("application/hal+json")]
		public IActionResult Get(int index = 0, int count = 10) {
			var items = db.ListVehicles().Skip(index).Take(count);
			var total = db.CountVehicles();
			var _links = Paginate("/api/vehicles", index, count, total);
			var _actions = new {
				create = new {
					method = "POST",
					type = "application/json",
					name = "Create a new vehicle",
					href = "/api/vehicles"
				},
				delete = new {
					method = "DELETE",
					name = "Delete a vehicle",
					href = "/api/vehicles/{id}"
				}
			};
			var result = new {
				_links, _actions, index, count, total, items
			};
			return Ok(result);
		}

		// GET api/vehicles/ABC123
		[HttpGet("{id}")]
		public IActionResult Get(string id) {
			var vehicle = db.FindVehicle(id);
			if (vehicle == default) return NotFound();
			var json = vehicle.ToDynamic();
			if (vehicle.Owner == null)
			{
				json._links = new {
					self = new { href = $"/api/vehicles/{id}" },
					vehicleModel = new { href = $"/api/models/{vehicle.ModelCode}" }
				};
			}
			else
			{
				json._links = new
				{
					self = new { href = $"/api/vehicles/{id}" },
					vehicleModel = new { href = $"/api/models/{vehicle.ModelCode}" },
					owner = new { href = $"/api/owners/{vehicle.Owner.Registration}" }
				};
			}
			json._actions = new {
				update = new {
					method = "PUT",
					href = $"/api/vehicles/{id}",
					accept = "application/json"
				},
				delete = new {
					method = "DELETE",
					href = $"/api/vehicles/{id}"
				}
			};
			return Ok(json);
		}

		// POST api/vehicles
		[HttpPost]
		public async Task<IActionResult> Post([FromBody] VehicleDto dto) {
			var vehicleModel = db.FindModel(dto.ModelCode);
			var vehicle = new Vehicle {
				Registration = dto.Registration,
				Color = dto.Color,
				Year = dto.Year,
				VehicleModel = vehicleModel
			};
			db.CreateVehicle(vehicle);
			
			return Ok(dto);
		}

		// PUT api/vehicles/ABC123
		[HttpPut("{id}")]
		public IActionResult Put(string id, [FromBody] dynamic dto) {
			var vehicleModelHref = dto._links.vehicleModel.href;
			var vehicleModelId = ModelsController.ParseModelId(vehicleModelHref);
			var vehicleModel = db.FindModel(vehicleModelId);
			var vehicle = new Vehicle {
				Registration = id,
				Color = dto.color,
				Year = dto.year,
				ModelCode = vehicleModel.Code
			};
			db.UpdateVehicle(vehicle);
			return Get(id);
		}

		// DELETE api/vehicles/ABC123
		[HttpDelete("{id}")]
		public IActionResult Delete(string id) {
			var vehicle = db.FindVehicle(id);
			if (vehicle == default) return NotFound();
			db.DeleteVehicle(vehicle);
			return NoContent();
		}
		
		public static string ParseVehicleId(dynamic href) {
			var tokens = ((string)href).Split("/");
			return tokens.Last();
		}
	}
}
