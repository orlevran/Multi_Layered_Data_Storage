Multi-Layered Data Storage API

A small ASP.NET Core Web API that demonstrates multi-layered persistence (Redis cache → JSON file → MongoDB) with read-through and write-through behavior, secured with JWT Bearer auth and role-based authorization. The project also includes CORS configuration and Swagger (OpenAPI) for interactive docs.

Features:
* Storage layers:
	* CacheStorage → IDistributedCache (Redis)
	* FileStorage → JSON file (UsersStorage.json)
	* DatabaseStorage → MongoDB via IUserRepository
* LoggingStorageDecorator → timing + structured logs for any storage
* Factory Patterns to create storage:
	* Repository Pattern for DB
 	* Decorator Pattern for logging
* JWT auth with role claim (Admin / User) + policy [Authorize(Roles="Admin")]
* CORS allow-list driven by configuration
* Swagger UI (/swagger) with Authorize button

Postman requests collection:
(Important note: In the Edit user's request, the field: request.auth.bearer.value
This string is the token that, in local runs, was provided for the user whose details are stored in this request.
The token is supposed to be valid for that user, but if there’s any issue, you can supply a new token via the Login request and place the returned token value in the appropriate field.

The users are stored in the DB with the IDs:
68cd38bd720d54d8e1b2312a
68cd38d5720d54d8e1b2312b


Additionally, there are two requests provided to test CORS Support.)
```json
{
	"info": {
		"_postman_id": "705aa656-a9d1-4797-89d0-6c60625f2517",
		"name": "Movement_Home_Task",
		"schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json",
		"_exporter_id": "20476221"
	},
	"item": [
		{
			"name": "Register",
			"request": {
				"method": "POST",
				"header": [
					{
						"key": "Content-Type",
						"value": "application/json",
						"type": "text"
					}
				],
				"body": {
					"mode": "raw",
					"raw": "{\r\n  \"role\": \"Admin\",\r\n  \"description\": \"Seed admin\"\r\n}",
					"options": {
						"raw": {
							"language": "json"
						}
					}
				},
				"url": {
					"raw": "https://localhost:7015/data",
					"protocol": "https",
					"host": [
						"localhost"
					],
					"port": "7015",
					"path": [
						"data"
					]
				}
			},
			"response": []
		},
		{
			"name": "Login",
			"protocolProfileBehavior": {
				"disableBodyPruning": true
			},
			"request": {
				"method": "GET",
				"header": [
					{
						"key": "Accept",
						"value": "application/json",
						"type": "text"
					}
				],
				"body": {
					"mode": "raw",
					"raw": "",
					"options": {
						"raw": {
							"language": "json"
						}
					}
				},
				"url": {
					"raw": "https://localhost:7015/data?id=68cc77513f6d2d564b91d028",
					"protocol": "https",
					"host": [
						"localhost"
					],
					"port": "7015",
					"path": [
						"data"
					],
					"query": [
						{
							"key": "id",
							"value": "68cc77513f6d2d564b91d028"
						}
					]
				}
			},
			"response": []
		},
		{
			"name": "Get User by Id",
			"protocolProfileBehavior": {
				"disableBodyPruning": true
			},
			"request": {
				"method": "GET",
				"header": [
					{
						"key": "Content-Type",
						"value": "application/json",
						"type": "text"
					}
				],
				"body": {
					"mode": "raw",
					"raw": "",
					"options": {
						"raw": {
							"language": "json"
						}
					}
				},
				"url": {
					"raw": "https://localhost:7015/data/68cd29fa8b0918fa1de1f400",
					"protocol": "https",
					"host": [
						"localhost"
					],
					"port": "7015",
					"path": [
						"data",
						"68cd29fa8b0918fa1de1f400"
					]
				}
			},
			"response": []
		},
		{
			"name": "Edit User",
			"request": {
				"auth": {
					"type": "bearer",
					"bearer": [
						{
							"key": "token",
							"value": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiI2OGNkMzhiZDcyMGQ1NGQ4ZTFiMjMxMmEiLCJodHRwOi8vc2NoZW1hcy5taWNyb3NvZnQuY29tL3dzLzIwMDgvMDYvaWRlbnRpdHkvY2xhaW1zL3JvbGUiOiJBZG1pbiIsImlhdCI6MTc1ODM1MzExNiwiY3JlYXRlZF9hdCI6IjIwMjUtMDktMTlUMTE6MDQ6MjkuNDQwMDAwMFoiLCJleHAiOjE3NjY5OTMxMTYsImlzcyI6Ik1vdmVtZW50LkhvbWUuVGFzay5BdXRoIiwiYXVkIjoiTW92ZW1lbnQuSG9tZS5UYXNrLkFwaSJ9.u9bA05DnTjQsTtS-lRDIpU5JTi17MR_a3h3849YeSUY",
							"type": "string"
						}
					]
				},
				"method": "PUT",
				"header": [
					{
						"key": "Content-Type",
						"value": "application/json",
						"type": "text"
					}
				],
				"body": {
					"mode": "raw",
					"raw": "{\r\n    \"Description\" : \"Edited\"\r\n}",
					"options": {
						"raw": {
							"language": "json"
						}
					}
				},
				"url": {
					"raw": "https://localhost:7015/data/68cd38d5720d54d8e1b2312b",
					"protocol": "https",
					"host": [
						"localhost"
					],
					"port": "7015",
					"path": [
						"data",
						"68cd38d5720d54d8e1b2312b"
					]
				}
			},
			"response": []
		},
		{
			"name": "CORS - Test (PUT)",
			"request": {
				"method": "GET",
				"header": [
					{
						"key": "Origin",
						"value": "http://localhost:5173",
						"type": "text"
					},
					{
						"key": "Access-Control-Request-Method",
						"value": "PUT",
						"type": "text"
					},
					{
						"key": "Access-Control-Request-Headers",
						"value": "content-type,authorization",
						"type": "text"
					}
				],
				"url": {
					"raw": "http://localhost:5215/data/68cd38bd720d54d8e1b2312a",
					"protocol": "http",
					"host": [
						"localhost"
					],
					"port": "5215",
					"path": [
						"data",
						"68cd38bd720d54d8e1b2312a"
					],
					"query": [
						{
							"key": "",
							"value": null,
							"disabled": true
						}
					]
				}
			},
			"response": []
		},
		{
			"name": "CORS - Test (PUT) 2",
			"request": {
				"method": "GET",
				"header": [
					{
						"key": "Origin",
						"value": "https://not-allowed.example",
						"type": "text"
					},
					{
						"key": "Access-Control-Request-Method",
						"value": "PUT",
						"type": "text"
					}
				],
				"url": {
					"raw": "http://localhost:5215/data/68cd38bd720d54d8e1b2312a",
					"protocol": "http",
					"host": [
						"localhost"
					],
					"port": "5215",
					"path": [
						"data",
						"68cd38bd720d54d8e1b2312a"
					],
					"query": [
						{
							"key": "",
							"value": null,
							"disabled": true
						}
					]
				}
			},
			"response": []
		}
	]
}
