Middleware is a great tool to inspect the pipeline in .net core. 

I have used this middleware class in a number of applications before... 

Just a note to get you setup. 



In your startup.cs file, in the 'Configure' method, you will need to add these lines. 

  //This must remain the first method to be called. Used for ServiceLoggingMiddleware
  app.Use(async (context, next) => {
     context.Request.EnableBuffering();
     await next();
  });
           
           
  //add toward to the bottom of the class         
  app.UseMiddleware<ServiceLoggingMiddleware>();
