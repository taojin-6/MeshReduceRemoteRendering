#include <cstdio>
#include <cstdlib>
#include <cstring>

#include <unistd.h>
#include <sys/socket.h>
#include <arpa/inet.h>

extern "C" {

int openConn(const char *host, uint16_t port) {
    int fd = socket(AF_INET, SOCK_STREAM, 0);
    if (fd < 0) {
        perror("socket");
        return -1;
    }

    sockaddr_in addr;
    memset(&addr, 0, sizeof(addr));
    addr.sin_family = AF_INET;
    inet_pton(AF_INET, host, &addr.sin_addr);
    addr.sin_port = htons(port);

    if (connect(fd, (sockaddr *)&addr, sizeof(addr)) < 0) {
        perror("connect");
        close(fd);
        return -1;
    }

    return fd;
}

void closeConn(int fd) {
    close(fd);
}

static int readAll(int fd, void *buf, size_t len) {
    while (len > 0) {
        ssize_t r = read(fd, buf, len);
        if (r <= 0) {
            perror("read");
            return -1;
        }
        buf = static_cast<char *>(buf) + r;
        len -= r;
    }
    return 0;
}

int readNF(int fd) {
    int nF;
    if (readAll(fd, &nF, sizeof(nF)) != 0)
        return -1;
    return nF;
}

int readMeshTexture(int fd, int nF, int tWidth, int tHeight, void *vertexBuf, uint32_t *indexBuf, void *textureBuf) {
    if (readAll(fd, vertexBuf, nF * 3 * 5 * sizeof(float)) != 0)
        return -1;
    if (readAll(fd, textureBuf, tWidth * tHeight * 3) != 0)
        return -1;
    for (int i = 0; i < 3 * nF; i++)
        indexBuf[i] = i;
    return 0;
}

}
