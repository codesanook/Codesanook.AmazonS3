using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Codesanook.AmazonS3.Services;
using Orchard.Commands;

namespace Codesanook.AmazonS3.Commands
{
    public class AmazonS3Commands : DefaultOrchardCommandHandler
    {
        private readonly IAmazonS3StorageProvider _amazonS3StorageProvider;

        public AmazonS3Commands(IAmazonS3StorageProvider amazonS3StorageProvider)
        {
            _amazonS3StorageProvider = amazonS3StorageProvider;
        }
    }
}