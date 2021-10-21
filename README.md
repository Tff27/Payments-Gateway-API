# Payment Gateway​
Rest **Payment Gateway** API to simulate a flow where a merchant will be allowed to process payments.

![Build](https://github.com/Tff27/Payments-Gateway-API/actions/workflows/dotnet.yml/badge.svg)
​
## Stack​
- .net 5.0
- MongoDB
​
## Run the application​

Update database connection string on appSettings with a valid connection string.

Go to the project root folder and head to  **src/Presentation.Api**  subfolder and run:
> dotnet run
​

This will start the API application (with development environment settings) on  [https://localhost:5001](https://localhost:5001/)  by default.

Or

Using Docker:

> docker build -t Presentation.API .
> docker run -d -p 5001:80 --name myapp Presentation.API
​
## Using the application​
Access Swagger  [https://localhost:5001/swagger](https://localhost:5001/swagger).
​
This will open the Swagger UI and will let us do the requests we want to test. 
​
-   **[No Authentication Required]**
	- **/login** - Is a mocked authentication endpoint that will generate a JWT token, that can be used to authenticate the next api calls (Please use "checkout" as username).
-   **[Authentication Required]**
	- **/authorize** - Requests an authorization for a payment.
	- **/payments/void** - Voids a given authorization for a payment.
	- **/payments/capture** - Capture the money from the customers bank.
	- **/payments/refund** - Refunds the money taken from the customers bank.
​
## Assumptions
- Success/Error response are delivered through standard http status codes
	- 2XX - Success
	- 4XX or 5XX - Errors
- Customer account balance contains 100 of the given currency

​
## Hardcoded edge cases​
|Card Number	| Error Expected |
|---------------|----------------|
|4000000000000119|Authorization Failure|
|4000000000000259|Capture Failure|
|4000000000003238|Refund Failure|


## Possible improvements to solution
### Technical
- Add more unit tests - Add unit tests for untested class.
- Add integration tests - Add integration test project.
- Use Events driven - Use a message **Bus** or **Queue** for better scalability and fault tolerance.
- Use retry mechanisms - Handle possible temporary errors using banks gateways.
- Improve Containerization - Use a docker compose file to run the application and database.
- Create\Generate an SDK - Using the OpenAPI standards create or generate a SDK so other can use our API.
- Use resources - Remove hardcoded strings and replace them by resources that could have multiple languages.
- Encryption - Encrypt client card data before storing the data in the database.
- Replace Authentication and Authorization - Use an identity provider (ex.: azure AD, Okta, Auth0).
### Business
- Apply currency conversion.
- Apply fees to merchants payments.
- Anti-Fraud system - review cards against a blacklist.
