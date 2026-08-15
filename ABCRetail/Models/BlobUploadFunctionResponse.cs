using System;
using System.Collections.Generic;
using System.Text;

namespace ABCRetail.Models
{
    public class BlobUploadFunctionResponse
    {
        public bool Success { get; set; }

        public string Message { get; set; }
            = string.Empty;

        public string BlobName { get; set; }
            = string.Empty;

        public string BlobUrl { get; set; }
            = string.Empty;
    }
}
