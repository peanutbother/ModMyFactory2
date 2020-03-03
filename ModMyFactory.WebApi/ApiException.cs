//  Copyright (C) 2020 Mathis Rech
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.

using System;
using System.Net;

namespace ModMyFactory.WebApi
{
    public class ApiException : Exception
    {
        public static ApiException FromWebException(WebException ex)
        {
            var response = (HttpWebResponse)ex.Response;
            return (ex.Status, response.StatusCode) switch
            {
                (WebExceptionStatus.ProtocolError, HttpStatusCode.NotFound) => new ResourceNotFoundException(ex),
                (WebExceptionStatus.ProtocolError, HttpStatusCode.Unauthorized) => new AuthenticationFailureException(ex),
                (WebExceptionStatus.Timeout, _) => new TimeoutException(ex),
                (WebExceptionStatus.ConnectFailure, _) => new ConnectFailureException(ex),
                _ => new ApiException("General API exception") // No matching exception found
            };
        }

        protected ApiException(string message, Exception innerException = null)
            : base(message, innerException)
        { }
    }
}
