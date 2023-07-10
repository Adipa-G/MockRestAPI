# API Mocking Tool: Simulate REST API Calls for Development and Testing

## Description:
This is a powerful and flexible API mocking tool that enables developers and testers to simulate REST API calls with ease. Whether you're working on application development, testing different scenarios, or building integrations, this tool offers a convenient way to generate mock API responses.

## Key Features:

* Seamless Swagger Integration: Generate mock responses based on Swagger files (OpenAPI specifications) for quick and accurate simulation of REST APIs. Extract endpoint paths, HTTP methods, request parameters, and response schemas to generate realistic mock data.
* Dynamic API Call Registration: Register API calls dynamically, allowing users to define specific responses for different endpoints and scenarios. Customize the behaviour of the mocked API to match your requirements.
* Request Matching Mechanism: Effortlessly match incoming requests to the registered endpoints by comparing the request's path, HTTP method, query parameters, headers, and request body. Ensure the appropriate mock response is provided for each API call.

## Configuration
This tool has a few configurations to setup swagger files. You can also use this tool without using a swagger file just using the rest API to configure responses. Following section of the `appsettings.json` file is used to configure swagger definitions and also the saved requests and responses to be use in the startup.

```json
{
  "Endpoints": {
    "ApiDefSubFolderName": "",
    "MockApiCallsSubFolder": "",
    "APIs": []
  }
}
```

* Endpoints:ApiDefSubFolderName - the root folder where Api definitions are stored. Mostly these are swagger files.
* Endpoints:APIs - List of APIs to be registered. API can be either a local swagger file or a remote swagger file.

    Following example is how to use a local swagger file. The swagger location is relative to the Endpoints:ApiDefSubFolderName location.

    ```JSON
    {
        "SwaggerLocation": "petstore/swagger.json",
        "ApiName": "petstore"
    }
    ```

    In above examples the swagger UI is available from the `<base url>/petstore/swagger`. The mock API is available from `<base url>/petstore`.

    Following example is how to use a remote swagger file.
    
    ```JSON
    {
        "SwaggerLocation": "https://app.swaggerhub.com/apiproxy/registry/TSAISIDOROS/SySkaki/1.0.0",
        "ApiName": "chess"
    }
    ```
      
    In this example too Swagger UI, and mock endpoint has a similar behaviour similar to the first example.

* Endpoints:MockApiCallsSubFolder - Folder where requests and responses are stored, so that they can be loaded at the application startup.

## Running the tool
### Using the docker image
docker compose files are provided for following scenarios.

#### Use the image from the docker hub
```bash
docker compose -f docker-compose-image.yml up
```

#### Build and use the image locally
```bash
docker compose -f docker-compose-image.yml build
docker compose -f docker-compose-build.yml up
```


