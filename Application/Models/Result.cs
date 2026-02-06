using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Models
{
    public class Result<T>
    {
        public bool IsSuccess { get; set; }
        public T? Value { get; set; }
        public string? Message { get; set; }
        public string[]? Errors { get; set; }
        public static Result<T> Success(T value, string msg) => new Result<T>{IsSuccess = true, Value = value, Message = msg};
        public static Result<T> Success(string msg) => new Result<T>{IsSuccess = true, Message = msg};
        public static Result<T> Failure(string error) => new Result<T>{IsSuccess = false, Message = error};
        public static Result<T> Failure(string msg, IEnumerable<string> errors) => new Result<T>{IsSuccess = false, Message = msg, Errors = errors.ToArray()};

    }
}