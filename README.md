# MockRestAPI: Simulate REST API Calls for Development and Testing

## Introduction
MockRestAPI is a powerful and flexible tool that enables developers and testers to simulate REST API calls with ease. Whether you're working on application development, testing different scenarios, or building integrations, this tool offers a convenient way to generate mock API responses.

### Key Features
- **Seamless Swagger Integration:** Generate mock responses based on Swagger files (OpenAPI specifications) for quick and accurate simulation of REST APIs. Extract endpoint paths, HTTP methods, request parameters, and response schemas to generate realistic mock data.
- **Dynamic API Call Registration:** Register API calls dynamically, allowing users to define specific responses for different endpoints and scenarios. Customize the behavior of the mocked API to match your requirements.
- **Request Matching Mechanism:** Effortlessly match incoming requests to the registered endpoints by comparing the request's path, HTTP method, query parameters, headers, and request body. Ensure the appropriate mock response is provided for each API call.

## Usage Guide
The easiest way to use MockRestAPI is by using the Docker image. The image is hosted on Docker Hub and can be used to run MockRestAPI. You can configure the Docker image using environment variables and mounted volumes. An example Docker Compose file (`docker-compose-image.yml`) is provided to demonstrate how to set up a MockRestAPI instance using environment variables and volumes.

### Configuration
MockRestAPI allows you to host a mock API using a Swagger file. The Swagger file can be either a file on the file system or a hosted file. The list of APIs can be defined as an array and provided as environment variables to the Docker container. Physical Swagger files should be placed inside a folder defined by the `Endpoints__ApiDefSubFolderName` environment variable. In the examples, we have mounted the folder as a volume in the Docker Compose.

#### Example: Physical Swagger File
```bash
Endpoints__APIs__0__SwaggerLocation="petstore/swagger.json"
Endpoints__APIs__0__ApiName="petstore"
```

In the above example, a physical Swagger file is provided for the API named petstore. The Swagger UI is available at `<base url>/petstore/swagger`, and the mock API is available at `<base url>/petstore`.

#### Example: Remote Swagger File
```bash
Endpoints__APIs__1__SwaggerLocation="https://app.swaggerhub.com/apiproxy/registry/TSAISIDOROS/SySkaki/1.0.0"
Endpoints__APIs__1__ApiName="chess"
```

In the above example, a remote Swagger file is provided for the API named chess. The Swagger UI is available at `<base url>/chess/swagger`, and the mock API is available at `<base url>/chess`.

MockRestAPI also provides its own management REST API to register mock API calls. The Swagger UI for this API is available at `<base url>/management/swagger`.

API calls can also be loaded from disk, and the location of these files is defined using the environment variable Endpoints__MockApiCallsSubFolder.

## API Reference

### Management API

#### POST `/mock-call/{callId}`
This API allows registering a mock response for a given API call. The API does not need to be defined in the Swagger specifications. Any random API can be registered using this method.

##### Example:



#### POST `/mock-call/{callId}`


