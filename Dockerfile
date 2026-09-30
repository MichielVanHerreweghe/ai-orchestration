FROM python:3-alpine
WORKDIR /srv
RUN echo ok > index.html
EXPOSE 8000
CMD ["python", "-m", "http.server", "8000"]
