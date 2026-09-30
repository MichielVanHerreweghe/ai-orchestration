FROM golang:alpine AS build
WORKDIR /src
COPY main.go .
RUN CGO_ENABLED=0 go build -o /server main.go

FROM scratch
COPY --from=build /server /server
EXPOSE 8000
CMD ["/server"]
