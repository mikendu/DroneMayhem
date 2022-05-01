
import numpy as np
import math
import collections

dimensions = [ 'x', 'y', 'z', 'w' ]

def _scale_control_points(unscaled_points, scale):
    s = scale
    l_s = 1 - s
    p = unscaled_points

    result = [None] * 8

    result[0] = p[0]
    result[1] = l_s * p[0] + s * p[1]
    result[2] = l_s ** 2 * p[0] + 2 * l_s * s * p[1] + s ** 2 * p[2]
    result[3] = l_s ** 3 * p[0] + 3 * l_s ** 2 * s * p[
        1] + 3 * l_s * s ** 2 * p[2] + s ** 3 * p[3]
    result[4] = l_s ** 3 * p[7] + 3 * l_s ** 2 * s * p[
        6] + 3 * l_s * s ** 2 * p[5] + s ** 3 * p[4]
    result[5] = l_s ** 2 * p[7] + 2 * l_s * s * p[6] + s ** 2 * p[5]
    result[6] = l_s * p[7] + s * p[6]
    result[7] = p[7]

    return result


def convert(points, degree):
    n = degree
    result = make_array(4, degree + 1)

    for d in range(4):
        for j in range(n + 1):
            s = ''
            for i in range(j + 1):
                # numerator = group("-1^" + str(i + j) + " * " + points[i][d])
                # denominator = group(factorial(i) + " * " + factorial(j - i) )
                # new_term = group(divide(numerator, denominator))

                # new_term = ((-1) ** (i + j)) * points[i][d] / (
                #         math.factorial(i) * math.factorial(j - i))
                coeff = ((-1) ** (i + j)) / (math.factorial(i) * math.factorial(j - i))
                new_term = group(multiply(points[i][d], coeff))
                if (len(s) > 0):
                    s = add(s, new_term)
                else:
                    s = new_term

            # c = s * math.factorial(n) / math.factorial(n - j)
            # c = divide(multiply(group(s), factorial(n)), factorial(n - j))
            scalar = math.factorial(n) / math.factorial(n - j)
            c = multiply(group(s), scalar)
            result[d][j] = c

    return result

def add(base, new_piece):
    return base + ' + ' + str(new_piece)

def multiply(base, new_piece):
    return base + ' * ' + str(new_piece)

def divide(base, new_piece):
    return base + ' / ' + str(new_piece)

def factorial(number):
    return str(number) + '!'

def group(term):
    return "(" + str(term) + ')'

def make_array(rows, cols, fill = False):
    result = []
    for i in range(rows):
        row = []
        rowName = 'p' + str(i)
        for j in range(cols):
            if (fill):
                dimension = dimensions[j]
                cellName = rowName + '_' + str(dimension)
                row.append(cellName)
            else:
                row.append('')
        result.append(row)

    return result

def get_tabs(count):
    result = ''
    for i in range(count):
        result += "\t"
    return result


def print_array(items, depth = 0):
    print(get_tabs(depth) + "[")
    for item in items:
        if isinstance(item, list):
            print_array(item, depth + 1)
        else:
            print(get_tabs(depth + 1) + str(item))

    print(get_tabs(depth) + "]")


degree = 7
points = make_array(degree + 1, 4, True)


print_array(points)
# print("scaled : ", _scale_control_points(points, 2.0))
print("\n\n")

coefficients = convert(points, degree)
print_array(coefficients)

