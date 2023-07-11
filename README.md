# MockRestAPI: Simulate REST API Calls for Development and Testing

## Introduction
MockRestAPI is a powerful and flexible tool that enables developers and testers to simulate REST API calls with ease. Whether you're working on application development, testing different scenarios, or building integrations, this tool offers a convenient way to generate mock API responses.

### Key Features
- **Seamless Swagger Integration:** Generate mock responses based on Swagger files (OpenAPI specifications) for quick and accurate simulation of REST APIs. Extract endpoint paths, HTTP methods, request parameters, and response schemas to generate realistic mock data.
- **Dynamic API Call Registration:** Register API calls dynamically, allowing users to define specific responses for different endpoints and scenarios. Customize the behaviour of the mocked API to match your requirements.
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

In the above example, a physical Swagger file is provided for the API named `petstore`. The Swagger UI is available at `<base url>/petstore/swagger`, and the mock API is available at `<base url>/petstore`.

#### Example: Remote Swagger File
```bash
Endpoints__APIs__1__SwaggerLocation="https://app.swaggerhub.com/apiproxy/registry/TSAISIDOROS/SySkaki/1.0.0"
Endpoints__APIs__1__ApiName="chess"
```

In the above example, a remote Swagger file is provided for the API named chess. The Swagger UI is available at `<base url>/chess/swagger`, and the mock API is available at `<base url>/chess`.

MockRestAPI also provides its own management REST API to register mock API calls. The Swagger UI for this API is available at `<base url>/management/swagger`.

API calls can also be loaded from disk, and the location of these files is defined using the environment variable `Endpoints__MockApiCallsSubFolder`.

## API Reference

### Generating Responses from Swagger

This section describes the process of generating responses using Swagger specifications. The responses can be overridden using the Management API.

#### Using Examples

If the Swagger specification contains examples as shown in the following YAML snippet, and the request path has an exact match, the matching example is returned.

```json
"/pet/{petId}": {
    "get": {
        "summary": "Find pet by ID",
        "description": "Returns a single pet",
        "operationId": "getPetById",
        "parameters": [{
                "name": "petId",
                "in": "path",
                "description": "ID of pet to return",
                "required": true,
                "schema": {
                    "type": "integer",
                    "format": "int64"
                }
            }
        ],
        "responses": {
            "200": {
                "description": "successful operation",
                "content": {
                    "application/json": {
                        "schema": {
                            "$ref": "#/components/schemas/Pet"
                        },
                        "examples": {
                            "38": {
                                "summary": "Scooby Doo dog",
                                "value": {
                                    "id": 38,
                                    "name": "Scooby Doo",
                                    "category": {
                                        "id": 1,
                                        "name": "Dogs"
                                    },
                                    "status": "available"
                                }
                            },
                            "41": {
                                "$ref": "#/components/examples/dog-example"
                            }
                        }
                    }
                }
            }
        }
    }
}
"components": {
    "examples": {
        "dog-example": {
            "summary": "Toto dog",
            "value": {
                "id": 41,
                "name": "Toto",
                "category": {
                    "id": 1,
                    "name": "Dogs"
                },
                "status": "available"
            }
        }
    }
}
```

In this example, if the request is `/pet/38` or `/pet/41`, the matching example is returned.

#### Using a single example

If the Swagger specification contains a single example, it is returned for the corresponding example. If there are multiple examples for individual routes and none of the routes match, the default example is returned.

```json
"/pet/{petId}": {
    "get": {
        "summary": "Find pet by ID",
        "description": "Returns a single pet",
        "operationId": "getPetById",
        "parameters": [{
                "name": "petId",
                "in": "path",
                "description": "ID of pet to return",
                "required": true,
                "schema": {
                    "type": "integer",
                    "format": "int64"
                }
            }
        ],
        "responses": {
            "200": {
                "description": "successful operation",
                "content": {
                    "application/json": {
                        "schema": {
                            "$ref": "#/components/schemas/Pet"
                        },
                        "example": {
                            "id": 40,
                            "name": "Snowy",
                            "category": {
                                "id": 1,
                                "name": "Dogs"
                            },
                            "status": "available"
                        }
                    }
                }
            }
        }
    }
}
```

#### Using schema defaults

If the Swagger specification does not contain any matching examples, an example is constructed using the schema's default values.

```json
"/pet/{petId}": {
    "get": {
        "summary": "Find pet by ID",
        "description": "Returns a single pet",
        "operationId": "getPetById",
        "parameters": [{
                "name": "petId",
                "in": "path",
                "description": "ID of pet to return",
                "required": true,
                "schema": {
                    "type": "integer",
                    "format": "int64"
                }
            }
        ],
        "responses": {
            "200": {
                "description": "successful operation",
                "content": {
                    "application/json": {
                        "schema": {
                            "$ref": "#/components/schemas/Pet"
                        }
                    }
                }
            }
        }
    }
}
"components": {
    "schemas": {
        "Category": {
            "type": "object",
            "properties": {
                "id": {
                    "type": "integer",
                    "format": "int64",
                    "example": 1
                },
                "name": {
                    "type": "string",
                    "example": "Dogs"
                }
            }
        },
        "Tag": {
            "type": "object",
            "properties": {
                "id": {
                    "type": "integer",
                    "format": "int64"
                },
                "name": {
                    "type": "string"
                }
            }
        },
        "Pet": {
            "required": [
                "name",
                "photoUrls"
            ],
            "type": "object",
            "properties": {
                "id": {
                    "type": "integer",
                    "format": "int64",
                    "example": 10
                },
                "name": {
                    "type": "string",
                    "example": "doggie"
                },
                "category": {
                    "$ref": "#/components/schemas/Category"
                },
                "photoUrls": {
                    "type": "array",
                    "xml": {
                        "wrapped": true
                    },
                    "items": {
                        "type": "string",
                        "xml": {
                            "name": "photoUrl"
                        }
                    }
                },
                "tags": {
                    "type": "array",
                    "xml": {
                        "wrapped": true
                    },
                    "items": {
                        "$ref": "#/components/schemas/Tag"
                    }
                },
                "status": {
                    "type": "string",
                    "description": "pet status in the store",
                    "enum": [
                        "available",
                        "pending",
                        "sold"
                    ],
                    "example": "available"
                }
            }
        }
    }
}
```
In this example, the response is constructed based on the schema's default values:

```json
{
    "id": 10,
    "name": "doggie",
    "category": {
        "id": 1,
        "name": "Dogs"
    },
    "status": "available"
}
```

### Management API

#### Registering a call

This API allows registering a mock response for a given API call. The API does not need to be defined in the Swagger specifications. Any random API can be registered using this method.

```bash
curl --location --request POST '<base url>/management/mock-call/12439' \
--header 'Content-Type: application/json' \
--data-raw '{
    "callId": "12439",
    "apiName": "petstore",
    "apiPath": "/pet",
    "method": "post",
    "queryParamsToMatch": [{
        "key": "status",
        "value": "sold"
    }],
    "headersToMatch": [{
        "key": "Authorization",
        "value": "Bearer 1234345533443"
    }],
    "bodyPathsToMatch": [{
        "key": "id",
        "value": "38"
    }],
    "timeToLive": 360,
    "responseCode": 200,
    "response": {
        "id": 38,
        "name": "Scooby Doo",
        "category": {
            "id": 1,
            "name": "Dogs"
        },
        "status": "available"
    },
    "returnOnlyForNthMatch": null,
    "matchCount": 0
}'
```

* **callId**: The unique ID for the API call.
* **apiName**: The name of the API to register the call against. It can be a known API name or any arbitrary name.
* **apiPath**: The API path to register the call against.
* **method**: The HTTP method to register the call against.
* **queryParamsToMatch**: An array of query parameters to match. If different values are passed for the same parameter, any value match is sufficient. For different parameters, all values need to match.
* **headersToMatch**:  An array of headers to match. If different values are passed for the same header name, any value match is sufficient. For different headers, all values need to match.
* **bodyPathsToMatch**: An array of body paths to match. If different values are passed for the same path, any value match is sufficient. For different paths, all values need to match.
* **timeToLive**: The number of seconds for which this response will be valid. Once the time expires, this mock call is removed.
* **returnOnlyForNthMatch**: The number of matches before the response is sent. This is useful when a different response is required for subsequent calls. Set to null if not needed.
* **responseCode**: The HTTP status code to be returned.
* **response**: The JSON payload to be returned as the response.

#### Getting a call

This API retrieves the registered API call.

```bash
curl --location --request GET '<base url>/management/mock-call/12439'
```

#### Deleting a call

This API unregisters a mock response for a given API call.

```bash
curl --location --request DELETE '<base url>/management/mock-call/12439'
```

#### Get all calls

This API retrieves a list of all registered mock calls.

```bash
curl --location --request GET '<base url>/management/mock-calls'
```

#### Reset all calls

This API resets the server to its initial state at start up including loading the JSON files located in the folder defined in the path `Endpoints__MockApiCallsSubFolder`. 

```bash
curl --location --request POST '<base url>/management/reset'
```

### File Format to Load at Startup

The MockRestAPI application supports loading mock API calls from files at startup. These files should be placed in the folder defined by the `Endpoints__MockApiCallsSubFolder` environment variable, and the application will automatically load them during startup.

The file format should be in JSON and follow the structure below:

```json
{
    "<API name>": {
        "<endpoint name>": {
            "<HTTP method>": [{
                "callId": "12439",
                "apiName": "petstore",
                "apiPath": "/pet",
                "method": "post",
                "queryParamsToMatch": [{
                    "key": "status",
                    "value": "sold"
                }],
                "headersToMatch": [{
                    "key": "Authorization",
                    "value": "Bearer 1234345533443"
                }],
                "bodyPathsToMatch": [{
                    "key": "id",
                    "value": "38"
                }],
                "timeToLive": 360,
                "responseCode": 200,
                "response": {
                    "id": 38,
                    "name": "Scooby Doo",
                    "category": {
                        "id": 1,
                        "name": "Dogs"
                    },
                    "status": "available"
                },
                "returnOnlyForNthMatch": null,
                "matchCount": 0
            }]
        }
    }
}
```

Note:

Replace `<API name>`, `<endpoint name>`, and `<HTTP method>` with the actual names of the API, endpoint, and HTTP method, respectively.
The structure can contain multiple API names, endpoint names, and HTTP methods to define multiple mock API calls in the file.
The JSON structure should match the format provided, including the placement of square brackets and curly braces.
Place the JSON file in the specified folder, and the MockRestAPI application will load and use the defined mock API calls during startup.

Make sure to restart the application or use the reset API call after adding or modifying the JSON file to apply the changes.

