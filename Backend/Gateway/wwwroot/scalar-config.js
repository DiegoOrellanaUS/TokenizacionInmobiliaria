export default {
  customFetch(input, init) {
    const request = new Request(input, init);
    const url = new URL(request.url);

    if (
      url.origin === window.location.origin &&
      url.pathname.startsWith("/api/") &&
      !request.headers.has("X-Tenant")
    ) {
      const tenant = window.localStorage.getItem("scalar.tenant");
      if (tenant) {
        request.headers.set("X-Tenant", tenant);
      }
    }

    return fetch(request);
  },
};
