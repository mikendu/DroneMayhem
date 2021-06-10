
import numpy as np
import matplotlib.pyplot as plt

# find the a & b points
def get_bezier_coef(points):
    # since the formulas work given that we have n+1 points
    # then n must be this:
    n = len(points) - 1

    # build coefficents matrix
    C = 4 * np.identity(n)
    np.fill_diagonal(C[1:], 1)
    np.fill_diagonal(C[:, 1:], 1)
    C[0, 0] = 2
    C[n - 1, n - 1] = 7
    C[n - 1, n - 2] = 2

    # build points vector
    P = [2 * (2 * points[i] + points[i + 1]) for i in range(n)]
    P[0] = points[0] + 2 * points[1]
    P[n - 1] = 8 * points[n - 1] + points[n]

    # solve system, find a & b
    A = np.linalg.solve(C, P)
    B = [0] * n
    for i in range(n - 1):
        B[i] = 2 * points[i + 1] - A[i + 1]
    B[n - 1] = (A[n - 1] + points[n]) / 2
    print("\n\nA1:\n", B)
    return A, B

# returns the general Bezier cubic formula given 4 control points
def get_cubic(a, b, c, d):
    return lambda t: np.power(1 - t, 3) * a + 3 * np.power(1 - t, 2) * t * b + 3 * (1 - t) * np.power(t, 2) * c + np.power(t, 3) * d

# return one cubic curve for each consecutive points
def get_bezier_cubic(points):
    A, B = get_bezier_coef(points)
    return [
        get_cubic(points[i], A[i], B[i], points[i + 1])
        for i in range(len(points) - 1)
    ]

# evalute each cubic curve on the range [0, 1] sliced in n points
def evaluate_bezier(points, n):
    curves = get_bezier_cubic(points)
    return np.array([fun(t) for fun in curves for t in np.linspace(0, 1, n)])








# find the a & b points
def get_bezier_coef2(points):
    # since the formulas work given that we have n+1 points
    # then n must be this:
    n = len(points) - 1

    # build coefficents matrix
    C = 4 * np.identity(n)
    np.fill_diagonal(C[1:], 1)
    np.fill_diagonal(C[:, 1:], 1)
    C[0, 0] = 2
    C[n - 1, n - 1] = 7
    C[n - 1, n - 2] = 2


    # build points vector
    P = [2 * (2 * points[i] + points[i + 1]) for i in range(n)]
    P[0] = points[0] + 2 * points[1]
    P[n - 1] = 8 * points[n - 1] + points[n]
    P = np.array(P)

    px = P[:,0]
    py = P[:,1]

    print("\nPx:\n", px)
    print("\nPy:\n", py)

    # solve system, find a & b
    ax = np.linalg.solve(C, px)
    ay = np.linalg.solve(C, py)

    bx = [0] * n
    by = [0] * n
    for i in range(n - 1):
        bx[i] = 2 * points[i + 1][0] - ax[i + 1]
        by[i] = 2 * points[i + 1][1] - ay[i + 1]

    bx[n - 1] = (ax[n - 1] + points[n][0]) / 2
    by[n - 1] = (ay[n - 1] + points[n][1]) / 2

    A = np.array([[x, y] for x, y in zip(ax, ay)])
    B = np.array([[x, y] for x, y in zip(bx, by)])

    return A, B

# return one cubic curve for each consecutive points
def get_bezier_cubic2(points):
    A, B = get_bezier_coef2(points)
    return [
        get_cubic(points[i], A[i], B[i], points[i + 1])
        for i in range(len(points) - 1)
    ]

# evalute each cubic curve on the range [0, 1] sliced in n points
def evaluate_bezier2(points, n):
    curves = get_bezier_cubic2(points)
    return np.array([fun(t) for fun in curves for t in np.linspace(0, 1, n)])

# generate 5 (or any number that you want) random points that we want to fit (or set them youreself)
# points = np.random.rand(8, 2)
points = np.array([
    [0.464, 0.431],
    [0.905, 1.792],
    [1.237, 0.824],
    [2.002, 1.305],
    [2.362, 1.269],
    [2.419, 1.433]
])

# fit the points with Bezier interpolation
# use 50 points between each consecutive points to draw the curve
# path = evaluate_bezier(points, 50)
path2 = evaluate_bezier2(points, 50)

# extract x & y coordinates of points
x, y = points[:,0], points[:,1]
# px, py = path[:,0], path[:,1]
px2, py2 = path2[:,0], path2[:,1]

# plot
plt.figure(figsize=(11, 8))
plt.plot(px2, py2, 'r--')
# plt.plot(px, py, 'b:')
plt.plot(x, y, 'ko')
plt.show()