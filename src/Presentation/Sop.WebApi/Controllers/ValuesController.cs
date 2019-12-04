﻿using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Sop.WebApi.Services;

namespace Sop.WebApi
{
    /// <summary>
    /// Simple REST API controller that shows Autofac injecting dependencies.
    /// </summary>
    /// <seealso cref="Microsoft.AspNetCore.Mvc.Controller" />
    [ApiController]
    [Route("api/[controller]")]
    public class ValuesController : ControllerBase
    {
        private readonly IValuesService _valuesService;

        public ValuesController(IValuesService valuesService)
        {
            this._valuesService = valuesService;
        }

        // GET api/values
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return this._valuesService.FindAll();
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return this._valuesService.Find(id);
        }
    }
}