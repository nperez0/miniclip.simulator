# TODO List

## Do not forget
- [ ] Use options pattern for configuration

## Projects & Ideas
- [ ] Create an integration test base class
- [ ] Improve retry policy, I could use polly
- [ ] Implement deadletter 
- [ ] Implement a caching strategy, just to show how it works, maybe I can use redis or something like that using a decorator pattern
- [ ] I want to add an e2e test project
- [ ] Can prevent receiving events that are not interested in the read model project? Maybe using a different topic for each read model?
- [ ] Check how to trace events
- [ ] Prepare project to be deployed in azure or aws
- [ ] What could be a strategy for service discovery?
- [ ] Check launch darkly
- [ ] How it works a migration using EventStoreDB?
- [ ] How can I rebuild read models replayng events from the beginning?
- [ ] Create a separate project to manage teams
- [ ] Check anti patterns
- [ ] Make other domain aggregates react to domain events
- [ ] Resolve ConsumeException ErrorCode.UnknownTopicOrPart when using Kafka
- [ ] Avoid inheriting from INotification for domain events
- [ ] Separate write request from the query request? 
- [ ] Check performance on write side
- [ ] Gracefully shutdown
- [ ] Add useful open telemetry tags and metrics (for example I could add metrics for domain errors or conflicts)
- [ ] Continue improving Result

## In Progress
- [ ] Add health check to the web job project

## Completed
- [ ] 

## Notes
