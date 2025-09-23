# aspnetcore9-otel-grafana-postgres-mysql-redis_apicontagem
Exemplo de API REST criada com o .NET 9 + ASP.NET Core e utilizando distributed tracing com Grafana + OpenTelemetry (implementacao generica ou especifica do Grafana) + PostgreSQL + MySQL + Redis. Para uso com ambientes empregados em testes de observabilidade e disponibilizados via Docker Compose.

Repositórios com os scripts + Docker Compose para a criação dos ambientes que farão uso do OpenTelemetry, PostgreSQL, MySQL e Redis:
- [Jaeger](https://github.com/renatogroffe/dockercompose-opentelemetry-jaeger-postgres-mysql-redis)
- [Grafana](https://github.com/renatogroffe/dockercompose-opentelemetry-grafana-postgres-mysql-redis)
- [Elastic APM](https://github.com/renatogroffe/dockercompose-opentelemetry-elasticapm-postgres-mysql-redis)
- [Zipkin](https://github.com/renatogroffe/dockercompose-opentelemetry-zipkin-postgres-mysql-redis)

Repositórios com as outras aplicações utilizadas nos testes com tracing distribuído:
- [Console App de orquestração em .NET 9](https://github.com/renatogroffe/dotnet9-consoleapp-otel-grafana_consumoapis)
- [API REST criada com Node.js](https://github.com/renatogroffe/nodejs-otel_apiconsumobackend)
- [API REST criada com Java + Spring + Apache Camel](https://github.com/renatogroffe/java-spring-camel_apiconsumobackend)
